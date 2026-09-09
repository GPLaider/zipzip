using ZipZip.Application.Theme;
using ZipZip.Domain.Models;
using ZipZip.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ZipZip.App.Views;

public sealed partial class SettingsPage : Page
{
    private enum SettingsSection
    {
        Theme,
        Compression,
        Associations,
        ExplorerMenus,
    }

    private readonly Dictionary<string, CheckBox> _associationCheckBoxes = new(StringComparer.OrdinalIgnoreCase);
    private bool _associationWheelHooked;
    private bool _isInitializing;
    private SettingsSection _selectedSection = SettingsSection.Theme;
    private ThemeMode _selectedThemeMode = ThemeMode.System;
    private ArchiveFormat _selectedFormat = ArchiveFormat.Zip;
    private CompressionLevel _selectedLevel = CompressionLevel.Normal;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        EnsureAssociationWheelHandler();
        EnsureAssociationCheckBoxes();

        var preferences = await App.Services.UserPreferences.GetAsync();
        _selectedThemeMode = preferences.ThemeMode;
        _selectedFormat = preferences.DefaultArchiveFormat == ArchiveFormat.SevenZip
            ? ArchiveFormat.SevenZip
            : ArchiveFormat.Zip;
        _selectedLevel = preferences.DefaultCompressionLevel;

        var selectedAssociations = preferences.AssociatedArchiveExtensions is null
            ? FileAssociationCatalog.RecommendedExtensions
            : preferences.AssociatedArchiveExtensions;
        var shellMenus = preferences.ShellMenus ?? ShellMenuPreferences.Default;

        ApplyAssociationSelection(selectedAssociations);
        ApplyShellMenuSelection(shellMenus);
        RefreshChoiceStyles();
        RefreshSectionPresentation();
        _isInitializing = false;
        App.MainWindowInstance?.SetWindowTitle("\uC124\uC815 - ZipZip");
    }

    private void EnsureAssociationWheelHandler()
    {
        if (_associationWheelHooked)
        {
            return;
        }

        AssociationScrollViewer.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler(OnAssociationScrollViewerPointerWheelChanged),
            handledEventsToo: true);
        ScrollViewer.SetIsVerticalScrollChainingEnabled(AssociationScrollViewer, false);

        _associationWheelHooked = true;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance?.GoBack();
    }

    private void OnSectionTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _selectedSection = button.Tag?.ToString() switch
        {
            "Compression" => SettingsSection.Compression,
            "Associations" => SettingsSection.Associations,
            "ExplorerMenus" => SettingsSection.ExplorerMenus,
            _ => SettingsSection.Theme,
        };

        RefreshSectionPresentation();
    }

    private async void OnThemeChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || App.MainWindowInstance is null)
        {
            return;
        }

        _selectedThemeMode = button.Tag?.ToString() switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

        RefreshChoiceStyles();

        if (_isInitializing)
        {
            return;
        }

        await App.MainWindowInstance.ApplyThemeAsync(_selectedThemeMode);
        ShowSaved("\uD14C\uB9C8 \uC124\uC815\uC744 \uC800\uC7A5\uD588\uC2B5\uB2C8\uB2E4.");
    }

    private async void OnFormatChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _selectedFormat = button.Tag?.ToString() == "SevenZip"
            ? ArchiveFormat.SevenZip
            : ArchiveFormat.Zip;

        RefreshChoiceStyles();

        if (_isInitializing)
        {
            return;
        }

        await SaveCompressionDefaultsAsync();
    }

    private async void OnLevelChoiceClick(object sender, RoutedEventArgs e)
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

        if (_isInitializing)
        {
            return;
        }

        await SaveCompressionDefaultsAsync();
    }

    private void OnSelectRecommendedAssociationsClick(object sender, RoutedEventArgs e)
    {
        ApplyAssociationSelection(FileAssociationCatalog.RecommendedExtensions);
    }

    private void OnSelectAllAssociationsClick(object sender, RoutedEventArgs e)
    {
        ApplyAssociationSelection(FileAssociationCatalog.AllExtensions);
    }

    private void OnClearAssociationsClick(object sender, RoutedEventArgs e)
    {
        ApplyAssociationSelection(Array.Empty<string>());
    }

    private async void OnApplyAssociationsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedExtensions = GetSelectedAssociationExtensions();
            await App.Services.FileAssociations.ApplyAsync(selectedExtensions);
            await App.Services.UserPreferences.SaveAssociatedArchiveExtensionsAsync(selectedExtensions);

            if (selectedExtensions.Count == 0)
            {
                ShowSaved("\uD30C\uC77C \uC5F0\uACB0 \uC124\uC815\uC744 \uD574\uC81C\uD588\uC2B5\uB2C8\uB2E4.");
                return;
            }

            ShowSaved(
                "\uC120\uD0DD\uD55C \uD655\uC7A5\uBA85\uC744 ZipZip \uC5F4\uAE30 \uD6C4\uBCF4\uB85C \uB4F1\uB85D\uD588\uC2B5\uB2C8\uB2E4. " +
                "Windows\uC5D0\uC11C \uC774\uBBF8 \uB2E4\uB978 \uAE30\uBCF8 \uC571\uC73C\uB85C \uACE0\uC815\uB41C \uD655\uC7A5\uBA85\uC740 Windows \uAE30\uBCF8 \uC571 \uC124\uC815\uC5D0\uC11C \uB9C8\uBB34\uB9AC\uD574 \uC8FC\uC138\uC694.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OnOpenDefaultAppsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Services.FileAssociations.OpenDefaultAppsSettings();
            ShowInfo("Windows \uAE30\uBCF8 \uC571 \uC124\uC815\uC744 \uC5F4\uC5C8\uC2B5\uB2C8\uB2E4.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnApplyExplorerMenusClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var preferences = BuildShellMenuPreferences();
            await App.Services.ExplorerMenus.ApplyAsync(preferences);
            await App.Services.UserPreferences.SaveShellMenuPreferencesAsync(preferences);
            ShowSaved("\uD0D0\uC0C9\uAE30 \uBA54\uB274 \uC124\uC815\uC744 \uC801\uC6A9\uD588\uC2B5\uB2C8\uB2E4.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task SaveCompressionDefaultsAsync()
    {
        await App.Services.UserPreferences.SaveCompressionDefaultsAsync(_selectedFormat, _selectedLevel);
        ShowSaved("\uAE30\uBCF8 \uC555\uCD95 \uC124\uC815\uC744 \uC800\uC7A5\uD588\uC2B5\uB2C8\uB2E4.");
    }

    private void EnsureAssociationCheckBoxes()
    {
        if (_associationCheckBoxes.Count > 0)
        {
            return;
        }

        PopulateAssociationGrid(ArchiveExtensionsGrid, FileAssociationCatalog.ArchiveFileOptions);
        PopulateAssociationGrid(UnixArchiveExtensionsGrid, FileAssociationCatalog.UnixArchiveFileOptions);
        PopulateAssociationGrid(DiskImageExtensionsGrid, FileAssociationCatalog.DiskImageOptions);
    }

    private void PopulateAssociationGrid(Grid grid, IReadOnlyList<ArchiveAssociationOption> options)
    {
        const int columnCount = 3;

        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        var rowCount = (int)Math.Ceiling(options.Count / (double)columnCount);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var checkBox = new CheckBox
            {
                Content = option.Label,
                MinWidth = 108,
                Tag = option.Extension,
            };

            _associationCheckBoxes[option.Extension] = checkBox;
            Grid.SetRow(checkBox, index / columnCount);
            Grid.SetColumn(checkBox, index % columnCount);
            grid.Children.Add(checkBox);
        }
    }

    private void ApplyAssociationSelection(IEnumerable<string> selectedExtensions)
    {
        var selectedSet = selectedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _associationCheckBoxes)
        {
            pair.Value.IsChecked = selectedSet.Contains(pair.Key);
        }
    }

    private IReadOnlyList<string> GetSelectedAssociationExtensions()
    {
        return FileAssociationCatalog.AllExtensions
            .Where(extension => _associationCheckBoxes.TryGetValue(extension, out var checkBox) && checkBox.IsChecked == true)
            .ToArray();
    }

    private void ApplyShellMenuSelection(ShellMenuPreferences preferences)
    {
        ShowOpenWithZipZipCheckBox.IsChecked = preferences.ShowOpenWithZipZip;
        ShowExtractHereCheckBox.IsChecked = preferences.ShowExtractHere;
        ShowExtractNewFolderCheckBox.IsChecked = preferences.ShowExtractNewFolder;
        ShowCompressDialogCheckBox.IsChecked = preferences.ShowCompressDialog;
        ShowCompressZipCheckBox.IsChecked = preferences.ShowCompressZip;
        ShowCompressSevenZipCheckBox.IsChecked = preferences.ShowCompressSevenZip;
    }

    private ShellMenuPreferences BuildShellMenuPreferences()
    {
        return new ShellMenuPreferences(
            ShowOpenWithZipZip: ShowOpenWithZipZipCheckBox.IsChecked == true,
            ShowExtractHere: ShowExtractHereCheckBox.IsChecked == true,
            ShowExtractNewFolder: ShowExtractNewFolderCheckBox.IsChecked == true,
            ShowCompressDialog: ShowCompressDialogCheckBox.IsChecked == true,
            ShowCompressZip: ShowCompressZipCheckBox.IsChecked == true,
            ShowCompressSevenZip: ShowCompressSevenZipCheckBox.IsChecked == true);
    }

    private void OnAssociationScrollViewerPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var delta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var targetOffset = Math.Clamp(
            scrollViewer.VerticalOffset - (delta / 2.0),
            0,
            scrollViewer.ScrollableHeight);

        if (Math.Abs(targetOffset - scrollViewer.VerticalOffset) >= 0.5)
        {
            scrollViewer.ChangeView(null, targetOffset, null, disableAnimation: true);
        }

        e.Handled = true;
    }

    private void RefreshChoiceStyles()
    {
        ApplyChoiceStyle(ThemeSystemButton, _selectedThemeMode == ThemeMode.System);
        ApplyChoiceStyle(ThemeLightButton, _selectedThemeMode == ThemeMode.Light);
        ApplyChoiceStyle(ThemeDarkButton, _selectedThemeMode == ThemeMode.Dark);

        ApplyChoiceStyle(FormatZipButton, _selectedFormat == ArchiveFormat.Zip);
        ApplyChoiceStyle(FormatSevenZipButton, _selectedFormat == ArchiveFormat.SevenZip);

        ApplyChoiceStyle(LevelFastButton, _selectedLevel == CompressionLevel.Fast);
        ApplyChoiceStyle(LevelNormalButton, _selectedLevel == CompressionLevel.Normal);
        ApplyChoiceStyle(LevelMaximumButton, _selectedLevel == CompressionLevel.Maximum);
    }

    private void RefreshSectionPresentation()
    {
        ThemeSection.Visibility = _selectedSection == SettingsSection.Theme ? Visibility.Visible : Visibility.Collapsed;
        CompressionSection.Visibility = _selectedSection == SettingsSection.Compression ? Visibility.Visible : Visibility.Collapsed;
        FileAssociationsSection.Visibility = _selectedSection == SettingsSection.Associations ? Visibility.Visible : Visibility.Collapsed;
        ExplorerMenusSection.Visibility = _selectedSection == SettingsSection.ExplorerMenus ? Visibility.Visible : Visibility.Collapsed;

        ApplySectionStyle(ThemeSectionButton, _selectedSection == SettingsSection.Theme);
        ApplySectionStyle(CompressionSectionButton, _selectedSection == SettingsSection.Compression);
        ApplySectionStyle(FileAssociationsSectionButton, _selectedSection == SettingsSection.Associations);
        ApplySectionStyle(ExplorerMenusSectionButton, _selectedSection == SettingsSection.ExplorerMenus);
    }

    private void ApplyChoiceStyle(Button button, bool isSelected)
    {
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetItemStatus(button, isSelected ? "선택됨" : "선택 안 됨");
        button.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
            isSelected ? "ZipZipChoiceButtonSelectedStyle" : "ZipZipChoiceButtonStyle"];
    }

    private void ApplySectionStyle(Button button, bool isSelected)
    {
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetItemStatus(button, isSelected ? "현재 설정 항목" : "선택 안 됨");
        button.Style = (Style)Resources[
            isSelected ? "SettingsNavButtonSelectedStyle" : "SettingsNavButtonStyle"];
    }

    private void ShowSaved(string message)
    {
        SettingsInfoBar.Severity = InfoBarSeverity.Success;
        SettingsInfoBar.Message = message;
        SettingsInfoBar.IsOpen = true;
    }

    private void ShowInfo(string message)
    {
        SettingsInfoBar.Severity = InfoBarSeverity.Informational;
        SettingsInfoBar.Message = message;
        SettingsInfoBar.IsOpen = true;
    }

    private void ShowError(string message)
    {
        SettingsInfoBar.Severity = InfoBarSeverity.Error;
        SettingsInfoBar.Message = string.IsNullOrWhiteSpace(message)
            ? "\uC124\uC815\uC744 \uC801\uC6A9\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."
            : message;
        SettingsInfoBar.IsOpen = true;
    }
}
