using ZipZip.Application.Theme;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZipZip.App.Services;

public sealed class ThemeService
{
    private readonly UserPreferencesService _preferencesService;
    private ThemeMode _current = ThemeMode.System;

    public ThemeService(UserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public ThemeMode Current => _current;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var preferences = await _preferencesService.GetAsync(cancellationToken);
        _current = preferences.ThemeMode;
    }

    public async Task ApplyAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        _current = mode;
        await _preferencesService.SaveThemeModeAsync(mode, cancellationToken);
    }

    public void ApplyTo(Window window)
    {
        if (window.Content is not FrameworkElement root)
        {
            return;
        }

        var theme = _current switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        ApplyThemeToElement(root, theme);
    }

    public string GetDisplayLabel()
    {
        return _current switch
        {
            ThemeMode.Light => "라이트",
            ThemeMode.Dark => "다크",
            _ => "시스템",
        };
    }

    private static void ApplyThemeToElement(FrameworkElement element, ElementTheme theme)
    {
        element.RequestedTheme = theme;

        switch (element)
        {
            case Frame { Content: FrameworkElement frameContent } frame:
                frame.RequestedTheme = theme;
                ApplyThemeToElement(frameContent, theme);
                break;
            case ContentControl { Content: FrameworkElement content } contentControl:
                contentControl.RequestedTheme = theme;
                ApplyThemeToElement(content, theme);
                break;
            case Panel panel:
                foreach (var child in panel.Children.OfType<FrameworkElement>())
                {
                    ApplyThemeToElement(child, theme);
                }
                break;
        }
    }
}
