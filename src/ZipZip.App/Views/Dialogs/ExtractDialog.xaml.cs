using ZipZip.Domain.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ZipZip.App.Views.Dialogs;

public sealed partial class ExtractDialog : ContentDialog
{
    public ExtractDialog()
    {
        InitializeComponent();
    }

    public void InitializeForArchive(string archivePath)
    {
        var baseDirectory = Path.GetDirectoryName(archivePath) ?? Environment.CurrentDirectory;
        DestinationPathTextBox.Text = baseDirectory;
    }

    public ExtractionOptions BuildOptions()
    {
        return new ExtractionOptions(
            string.IsNullOrWhiteSpace(DestinationPathTextBox.Text)
                ? Environment.CurrentDirectory
                : DestinationPathTextBox.Text.Trim(),
            CreateNewFolderCheckBox.IsChecked == true,
            OverwriteExistingCheckBox.IsChecked == true);
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Downloads,
            CommitButtonText = "\uC120\uD0DD",
        };
        picker.FileTypeFilter.Add("*");

        if (App.MainWindowInstance is not null)
        {
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            DestinationPathTextBox.Text = folder.Path;
        }
    }
}
