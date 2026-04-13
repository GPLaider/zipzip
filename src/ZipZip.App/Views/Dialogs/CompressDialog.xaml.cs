using ZipZip.Domain.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ZipZip.App.Views.Dialogs;

public sealed partial class CompressDialog : ContentDialog
{
    private IReadOnlyList<string> _inputPaths = [];
    private ArchiveFormat _selectedFormat = ArchiveFormat.Zip;
    private CompressionLevel _selectedLevel = CompressionLevel.Normal;

    public CompressDialog()
    {
        InitializeComponent();
    }

    public void InitializeForInputPaths(IReadOnlyList<string> inputPaths)
    {
        _inputPaths = inputPaths;
        SelectedItemsText.Text = $"\uC120\uD0DD \uD56D\uBAA9 {inputPaths.Count}\uAC1C";

        if (inputPaths.Count == 0)
        {
            OutputDirectoryTextBox.Text = ResolveInitialOutputDirectory(null);
            ArchiveNameTextBox.Text = "archive.zip";
            RefreshChoiceStyles();
            return;
        }

        var preferences = App.Services.UserPreferences.Current;
        _selectedFormat = preferences.DefaultArchiveFormat == ArchiveFormat.SevenZip
            ? ArchiveFormat.SevenZip
            : ArchiveFormat.Zip;
        _selectedLevel = preferences.DefaultCompressionLevel;

        var firstPath = inputPaths[0];
        var outputDirectory = ResolveInitialOutputDirectory(firstPath);
        var baseName = Path.GetFileNameWithoutExtension(firstPath);

        OutputDirectoryTextBox.Text = outputDirectory;
        ArchiveNameTextBox.Text = _selectedFormat == ArchiveFormat.SevenZip
            ? $"{baseName}.7z"
            : $"{baseName}.zip";

        RefreshChoiceStyles();
    }

    public CompressionOptions BuildOptions()
    {
        var firstInputPath = _inputPaths.FirstOrDefault() ?? Environment.CurrentDirectory;
        var fallbackDirectory = Directory.Exists(firstInputPath)
            ? firstInputPath
            : Path.GetDirectoryName(firstInputPath) ?? Environment.CurrentDirectory;
        var outputDirectory = string.IsNullOrWhiteSpace(OutputDirectoryTextBox.Text)
            ? fallbackDirectory
            : OutputDirectoryTextBox.Text.Trim();

        var fileName = string.IsNullOrWhiteSpace(ArchiveNameTextBox.Text)
            ? "archive.zip"
            : ArchiveNameTextBox.Text.Trim();

        fileName = EnsureExtension(fileName, _selectedFormat);

        return new CompressionOptions(
            Path.Combine(outputDirectory, fileName),
            _selectedFormat,
            _selectedLevel,
            string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password,
            string.IsNullOrWhiteSpace(SplitSizeTextBox.Text) ? null : SplitSizeTextBox.Text.Trim(),
            EncryptFileNamesCheckBox.IsChecked == true);
    }

    private async void OnBrowseOutputDirectoryClick(object sender, RoutedEventArgs e)
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
            OutputDirectoryTextBox.Text = folder.Path;
        }
    }

    private void OnFormatChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _selectedFormat = button.Tag?.ToString() == "SevenZip"
            ? ArchiveFormat.SevenZip
            : ArchiveFormat.Zip;

        ArchiveNameTextBox.Text = EnsureExtension(
            string.IsNullOrWhiteSpace(ArchiveNameTextBox.Text) ? "archive.zip" : ArchiveNameTextBox.Text.Trim(),
            _selectedFormat);

        RefreshChoiceStyles();
    }

    private void OnLevelChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _selectedLevel = button.Tag?.ToString() switch
        {
            "Fast" => CompressionLevel.Fast,
            "Maximum" => CompressionLevel.Maximum,
            _ => CompressionLevel.Normal,
        };

        RefreshChoiceStyles();
    }

    private void RefreshChoiceStyles()
    {
        ApplyChoiceStyle(FormatZipButton, _selectedFormat == ArchiveFormat.Zip);
        ApplyChoiceStyle(FormatSevenZipButton, _selectedFormat == ArchiveFormat.SevenZip);
        ApplyChoiceStyle(LevelFastButton, _selectedLevel == CompressionLevel.Fast);
        ApplyChoiceStyle(LevelNormalButton, _selectedLevel == CompressionLevel.Normal);
        ApplyChoiceStyle(LevelMaximumButton, _selectedLevel == CompressionLevel.Maximum);
    }

    private void ApplyChoiceStyle(Button button, bool isSelected)
    {
        button.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
            isSelected ? "ZipZipChoiceButtonSelectedStyle" : "ZipZipChoiceButtonStyle"];
    }

    private static string EnsureExtension(string fileName, ArchiveFormat format)
    {
        var expectedExtension = format == ArchiveFormat.SevenZip ? ".7z" : ".zip";
        return Path.HasExtension(fileName)
            ? Path.ChangeExtension(fileName, expectedExtension)
            : $"{fileName}{expectedExtension}";
    }

    private static string ResolveInitialOutputDirectory(string? firstInputPath)
    {
        var rememberedDirectory = App.Services.UserPreferences.Current.LastCompressionOutputDirectory;
        if (!string.IsNullOrWhiteSpace(rememberedDirectory) && Directory.Exists(rememberedDirectory))
        {
            return rememberedDirectory;
        }

        if (!string.IsNullOrWhiteSpace(firstInputPath))
        {
            return Directory.Exists(firstInputPath)
                ? firstInputPath
                : Path.GetDirectoryName(firstInputPath) ?? Environment.CurrentDirectory;
        }

        return Environment.CurrentDirectory;
    }
}
