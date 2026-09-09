using ZipZip.App.Services;
using ZipZip.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace ZipZip.App.Views;

public sealed partial class HomePage : Page
{
    private HomeViewModel ViewModel => (HomeViewModel)DataContext;

    public HomePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var preferences = await App.Services.UserPreferences.GetAsync();
        ViewModel.LoadRecentArchives(preferences.RecentArchives);
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

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance?.ShowSettings();
    }

    private async void OnCreateFolderClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindowInstance is not null)
            await App.MainWindowInstance.PickFolderAndCreateArchiveAsync();
    }

    private void OnRecentItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentArchiveItem item)
        {
            var opened = App.MainWindowInstance?.ShowArchive(item.FullPath) ?? false;
            if (!opened)
            {
                SetHomeStatus("\uC555\uCD95 \uD30C\uC77C\uC744 \uC5F4\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.", InfoBarSeverity.Error);
            }
        }
    }

    private async void OnRemoveRecentItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string fullPath })
        {
            return;
        }

        await App.Services.UserPreferences.RemoveRecentArchiveAsync(fullPath);
        var preferences = await App.Services.UserPreferences.GetAsync();
        ViewModel.LoadRecentArchives(preferences.RecentArchives);
        SetHomeStatus("\uCD5C\uADFC \uAE30\uB85D\uC5D0\uC11C \uC81C\uAC70\uD588\uC2B5\uB2C8\uB2E4.", InfoBarSeverity.Success);
    }

    private void SetHomeStatus(string message, InfoBarSeverity severity)
    {
        HomeStatusInfoBar.Message = message;
        HomeStatusInfoBar.Severity = severity;
        HomeStatusInfoBar.IsOpen = true;
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
            SetHomeStatus("\uD30C\uC77C\uC774\uB098 \uD3F4\uB354\uB9CC \uBD99\uC5EC\uB123\uC744 \uC218 \uC788\uC2B5\uB2C8\uB2E4.", InfoBarSeverity.Warning);
            return;
        }

        if (App.MainWindowInstance is not null)
        {
            await App.MainWindowInstance.HandleIncomingPathsAsync(paths);
        }
    }
}
