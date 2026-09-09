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
    private ArchiveViewModel ViewModel => (ArchiveViewModel)DataContext;
    private string? _archivePassword;

    public ArchivePage()
    {
        InitializeComponent();
        EntriesListView.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnEntriesKeyDown), handledEventsToo: true);
        DataContext = new ArchiveViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string archivePath && !string.IsNullOrWhiteSpace(archivePath))
        {
            await ViewModel.LoadAsync(App.Services.OpenArchive, archivePath);

            while (ViewModel.HasError && SevenZipErrorClassifier.RequiresPassword(ViewModel.ErrorMessage ?? string.Empty))
            {
                _archivePassword = await RequestPasswordAsync("파일 목록이 암호화되어 있습니다. 암호를 입력해 주세요.");
                if (_archivePassword is null) return;
                await ViewModel.LoadAsync(App.Services.OpenArchive, archivePath, password: _archivePassword);
            }

            if (!ViewModel.HasError)
            {
                App.MainWindowInstance?.SetWindowTitle($"{ViewModel.ArchiveName} - ZipZip");

                try
                {
                    await App.Services.UserPreferences.AddRecentArchiveAsync(archivePath);
                }
                catch (Exception ex)
                {
                    ViewModel.SetError(ex.Message);
                }
            }
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

    private async void OnTestArchiveClick(object sender, RoutedEventArgs e)
    {
        while (true)
        {
            try
            {
                await App.MainWindowInstance!.RunArchiveOperationAsync("무결성 검사 중",
                    (token, progress) => App.Services.TestArchive.ExecuteAsync(ViewModel.ArchivePath, token, _archivePassword, progress));
                ViewModel.SetOperationSuccess("무결성 검사를 통과했습니다. 파일을 추출하지 않았습니다.");
                return;
            }
            catch (OperationCanceledException)
            {
                ViewModel.SetOperationInfo("무결성 검사를 중지했습니다.");
                return;
            }
            catch (Exception ex) when (NeedsPassword(ex))
            {
                _archivePassword = await RequestPasswordAsync("검사에 사용할 암호를 입력해 주세요. 이전 암호가 틀렸다면 다시 입력해 주세요.");
                if (_archivePassword is null) return;
            }
            catch (Exception ex)
            {
                ViewModel.SetOperationInfo("무결성 검사 실패: " + ex.Message, InfoBarSeverity.Error);
                return;
            }
        }
    }

    private async void OnExtractSelectedClick(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            ViewModel.SetOperationInfo("\uBA3C\uC800 \uD56D\uBAA9\uC744 \uC120\uD0DD\uD574 \uC8FC\uC138\uC694.");
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

    private void OnEntryContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(args.ItemContainer,
            args.InRecycleQueue ? string.Empty : (args.Item as ArchiveEntryItemViewModel)?.AccessibleName ?? string.Empty);
    }

    private void OnEntriesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateSelection(GetSelectedItems());
    }

    private async void OnEntriesDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await OpenSelectedAsync();
    }

    private async void OnEntriesKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await OpenSelectedAsync();
        }
        else if (e.Key == Windows.System.VirtualKey.Back)
        {
            e.Handled = true;
            ViewModel.NavigateUp();
        }
    }

    private async Task OpenSelectedAsync()
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
        e.DragUIOverride.Caption = "\uB193\uC73C\uBA74 \uC5F4\uAC70\uB098 \uC0C8 \uC555\uCD95\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.";
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
        ViewModel.SetOperationInfo($"\uC120\uD0DD\uD55C {selectedItems.Count}\uAC1C \uD56D\uBAA9 \uACBD\uB85C\uB97C \uBCF5\uC0AC\uD588\uC2B5\uB2C8\uB2E4.");
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
            ViewModel.SetOperationInfo("\uD30C\uC77C\uC774\uB098 \uD3F4\uB354\uB9CC \uBD99\uC5EC\uB123\uC744 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
            return;
        }

        if (App.MainWindowInstance is not null)
        {
            await App.MainWindowInstance.HandleIncomingPathsAsync(paths);
        }
    }

    private async Task ExecuteExtractionWithPasswordRetryAsync(ExtractionOptions options, int selectedCount)
    {
        var currentOptions = options with { Password = _archivePassword };
        var completedFolderPath = SevenZipArchiveBackend.ResolveExtractionDestinationPath(ViewModel.ArchivePath, currentOptions);
        // Resolve once so password retries use the same folder and the result link stays correct.
        currentOptions = currentOptions with { DestinationPath = completedFolderPath, CreateNewFolder = false };
        var promptMessage = "\uC554\uD638\uAC00 \uD544\uC694\uD55C \uC555\uCD95 \uD30C\uC77C\uC785\uB2C8\uB2E4. \uC554\uD638\uB97C \uC785\uB825\uD574 \uC8FC\uC138\uC694.";

        while (true)
        {
            try
            {
                await App.MainWindowInstance!.RunArchiveOperationAsync("압축 푸는 중",
                    (token, progress) => App.Services.ExtractArchive.ExecuteAsync(ViewModel.ArchivePath, currentOptions, token, progress));
                App.MainWindowInstance?.SetWindowTitle($"{ViewModel.ArchiveName} - ZipZip");
                ViewModel.SetOperationSuccess(selectedCount == 0
                    ? "\uC555\uCD95 \uD480\uAE30\uAC00 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4."
                    : $"\uC120\uD0DD\uD55C {selectedCount}\uAC1C \uD56D\uBAA9\uC744 \uD480\uC5C8\uC2B5\uB2C8\uB2E4.",
                    completedFolderPath);
                return;
            }
            catch (OperationCanceledException)
            {
                ViewModel.SetOperationInfo("압축 풀기를 중지했습니다. 이미 풀린 파일은 대상 폴더에 남아 있습니다.");
                return;
            }
            catch (Exception ex) when (NeedsPassword(ex))
            {
                var password = await RequestPasswordAsync(promptMessage);
                if (password is null)
                {
                    ViewModel.SetOperationInfo("\uC555\uCD95 \uD480\uAE30\uB97C \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.");
                    return;
                }

                currentOptions = currentOptions with { Password = password };
                _archivePassword = password;
                promptMessage = "\uC554\uD638\uAC00 \uC62C\uBC14\uB974\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uC785\uB825\uD574 \uC8FC\uC138\uC694.";
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

}
