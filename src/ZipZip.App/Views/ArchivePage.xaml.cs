using ZipZip.App.Services;
using ZipZip.App.ViewModels;
using ZipZip.App.Views.Dialogs;
using ZipZip.ArchiveAdapters.SevenZip;
using ZipZip.Domain.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace ZipZip.App.Views;

public sealed partial class ArchivePage : Page
{
    private string? _archivePassword;
    private ArchiveViewModel ViewModel => (ArchiveViewModel)DataContext;

    public ArchivePage()
    {
        InitializeComponent();
        DataContext = new ArchiveViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string archivePath && !string.IsNullOrWhiteSpace(archivePath))
        {
            await LoadArchiveWithPasswordRetryAsync(archivePath, addToRecents: true);
        }
    }

    private async void OnOpenArchiveClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindowInstance is not null)
        {
            await App.MainWindowInstance.OpenArchivePickerAsync();
        }
    }

    private async void OnCreateArchiveClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindowInstance is not null)
        {
            await App.MainWindowInstance.PickInputsAndCreateArchiveAsync();
        }
    }

    private void OnNavigateUpClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.NavigateUp())
        {
            App.MainWindowInstance?.GoBack();
        }
    }

    private void OnHomeClick(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance?.ShowHome();
    }

    private async void OnExtractClick(object sender, RoutedEventArgs e)
    {
        await ExtractItemsAsync([]);
    }

    private async void OnExtractSelectedClick(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            ViewModel.SetOperationInfo("먼저 항목을 선택해 주세요.");
            return;
        }

        await ExtractItemsAsync(selectedItems);
    }

    private async Task ExtractItemsAsync(IReadOnlyList<ArchiveEntryItemViewModel> selectedItems)
    {
        var dialog = new ExtractDialog
        {
            XamlRoot = XamlRoot,
        };
        dialog.InitializeForArchive(ViewModel.ArchivePath);

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var options = dialog.BuildOptions() with
        {
            SelectedEntries = selectedItems.Select(item => item.InternalPath).ToArray(),
        };

        await ExecuteExtractionWithPasswordRetryAsync(options, selectedItems.Count);
    }

    private void OnEntriesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateSelection(GetSelectedItems());
    }

    private async void OnEntriesDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count != 1)
        {
            return;
        }

        var item = selectedItems[0];
        if (item.IsDirectory)
        {
            ViewModel.NavigateInto(item);
            return;
        }

        await ExtractItemsAsync(selectedItems);
    }

    private List<ArchiveEntryItemViewModel> GetSelectedItems()
    {
        return EntriesListView.SelectedItems
            .OfType<ArchiveEntryItemViewModel>()
            .ToList();
    }

    private void OnBreadcrumbClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string folderPath })
        {
            ViewModel.NavigateToBreadcrumb(folderPath);
        }
    }

    private void OnRootDragOver(object sender, DragEventArgs e)
    {
        if (!IncomingItemService.CanAccept(e.DataView))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.Caption = "놓으면 열거나 새 압축을 시작합니다.";
    }

    private async void OnRootDrop(object sender, DragEventArgs e)
    {
        await HandleIncomingDataAsync(e.DataView);
    }

    private void OnCopyKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(string.Join(Environment.NewLine, selectedItems.Select(item => item.InternalPath)));
        Clipboard.SetContent(package);
        ViewModel.SetOperationInfo($"선택한 {selectedItems.Count}개 항목 경로를 복사했습니다.");
        args.Handled = true;
    }

    private async void OnPasteKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await HandleIncomingDataAsync(Clipboard.GetContent());
    }

    private async Task HandleIncomingDataAsync(DataPackageView dataView)
    {
        var paths = await IncomingItemService.GetPathsAsync(dataView);
        if (paths.Count == 0)
        {
            ViewModel.SetOperationInfo("파일이나 폴더만 붙여넣을 수 있습니다.");
            return;
        }

        if (App.MainWindowInstance is not null)
        {
            await App.MainWindowInstance.HandleIncomingPathsAsync(paths);
        }
    }

    private async Task ExecuteExtractionWithPasswordRetryAsync(ExtractionOptions options, int selectedCount)
    {
        var currentOptions = PrepareExtractionOptions(options);
        var completedFolderPath = currentOptions.DestinationPath;
        var promptMessage = "암호가 필요한 압축 파일입니다. 암호를 입력해 주세요.";

        while (true)
        {
            try
            {
                await App.Services.ExtractArchive.ExecuteAsync(ViewModel.ArchivePath, currentOptions);
                _archivePassword = currentOptions.Password;
                await LoadArchiveWithPasswordRetryAsync(ViewModel.ArchivePath, addToRecents: false);
                ViewModel.SetOperationSuccess(
                    selectedCount == 0
                        ? "압축 풀기가 완료되었습니다."
                        : $"선택한 {selectedCount}개 항목을 풀었습니다.",
                    completedFolderPath);
                return;
            }
            catch (Exception ex) when (NeedsPassword(ex))
            {
                var password = await RequestPasswordAsync(promptMessage);
                if (password is null)
                {
                    ViewModel.SetOperationInfo("압축 풀기를 취소했습니다.");
                    return;
                }

                currentOptions = currentOptions with { Password = password };
                promptMessage = "암호가 올바르지 않습니다. 다시 입력해 주세요.";
            }
            catch (Exception ex)
            {
                ViewModel.SetError(ex.Message);
                return;
            }
        }
    }

    private async Task<string?> RequestPasswordAsync(string message)
    {
        var dialog = new PasswordPromptDialog
        {
            XamlRoot = XamlRoot,
        };
        dialog.Initialize(message);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? dialog.Password : null;
    }

    private static bool NeedsPassword(Exception exception)
    {
        return SevenZipErrorClassifier.RequiresPassword(exception.Message);
    }

    private async Task LoadArchiveWithPasswordRetryAsync(string archivePath, bool addToRecents)
    {
        var currentPassword = _archivePassword;
        var promptMessage = "암호가 필요한 압축 파일입니다. 암호를 입력해 주세요.";

        while (true)
        {
            try
            {
                await ViewModel.LoadAsync(
                    App.Services.OpenArchive,
                    archivePath,
                    currentPassword,
                    rethrowOnError: true);

                _archivePassword = currentPassword;
                App.MainWindowInstance?.SetWindowTitle($"{ViewModel.ArchiveName} - ZipZip");

                if (!addToRecents)
                {
                    return;
                }

                try
                {
                    await App.Services.UserPreferences.AddRecentArchiveAsync(archivePath);
                }
                catch (Exception ex)
                {
                    ViewModel.SetError(ex.Message);
                }

                return;
            }
            catch (Exception ex) when (NeedsPassword(ex))
            {
                var password = await RequestPasswordAsync(promptMessage);
                if (password is null)
                {
                    App.MainWindowInstance?.ShowHome();
                    return;
                }

                currentPassword = password;
                promptMessage = "암호가 올바르지 않습니다. 다시 입력해 주세요.";
            }
            catch
            {
                return;
            }
        }
    }

    private ExtractionOptions PrepareExtractionOptions(ExtractionOptions options)
    {
        var effectivePassword = string.IsNullOrWhiteSpace(options.Password)
            ? _archivePassword
            : options.Password;
        var destinationPath = SevenZipArchiveBackend.ResolveExtractionDestinationPath(
            ViewModel.ArchivePath,
            options with { Password = effectivePassword });

        return options with
        {
            DestinationPath = destinationPath,
            CreateNewFolder = false,
            Password = effectivePassword,
        };
    }
}
