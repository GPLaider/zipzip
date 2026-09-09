using ZipZip.App.Services;
using ZipZip.App.Visuals;
using ZipZip.App.Views;
using ZipZip.App.Views.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ZipZip.App;

public sealed partial class MainWindow : Window
{
    private readonly ThemeService _themeService;
    private bool _initialWindowSizeApplied;
    private bool _windowActive;

    public MainWindow(ThemeService themeService)
    {
        InitializeComponent();
        _themeService = themeService;
        _themeService.ApplyTo(this);
        ApplyWindowIcon();
        Activated += OnInitialWindowActivated;
        Activated += (_, args) =>
        {
            _windowActive = args.WindowActivationState != WindowActivationState.Deactivated;
            if (!_windowActive) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_windowActive || RootFrame.XamlRoot is null) return;
                var target = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(RootFrame.XamlRoot) as Control
                    ?? Microsoft.UI.Xaml.Input.FocusManager.FindFirstFocusableElement(RootFrame) as Control;
                target?.Focus(FocusState.Programmatic);
            });
        };
        RootFrame.Navigated += OnRootFrameNavigated;
    }

    public void ShowHome()
    {
        SetWindowTitle("ZipZip");
        RootFrame.Navigate(typeof(HomePage));
    }

    public bool ShowArchive(string archivePath = "sample.zip")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(archivePath))
            {
                throw new ArgumentException("Archive path is empty.", nameof(archivePath));
            }

            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException("Archive file was not found.", archivePath);
            }

            SetWindowTitle($"{Path.GetFileName(archivePath)} - ZipZip");

            if (!RootFrame.Navigate(typeof(ArchivePage), archivePath))
            {
                throw new InvalidOperationException("Archive page navigation returned false.");
            }

            return true;
        }
        catch (Exception ex)
        {
            App.ReportException($"MainWindow.ShowArchive:{archivePath}", ex);
            SetWindowTitle("ZipZip");

            try
            {
                RootFrame.Navigate(typeof(HomePage));
            }
            catch
            {
            }

            return false;
        }
    }

    public void ShowSettings()
    {
        SetWindowTitle("\uC124\uC815 - ZipZip");
        RootFrame.Navigate(typeof(SettingsPage));
    }

    public async Task OpenArchivePickerAsync()
    {
        var picker = new FileOpenPicker();
        foreach (var extension in ArchiveFormatAssetCatalog.SupportedOpenArchiveExtensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ShowArchive(file.Path);
        }
    }

    public async Task PickInputsAndCreateArchiveAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count == 0)
        {
            return;
        }

        await ShowCreateArchiveDialogAsync(files.Select(file => file.Path).ToArray());
    }

    public async Task PickFolderAndCreateArchiveAsync()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            await ShowCreateArchiveDialogAsync([folder.Path]);
    }

    public async Task ShowCreateArchiveDialogAsync(IReadOnlyList<string> inputPaths)
    {
        if (inputPaths.Count == 0)
        {
            ShowHome();
            return;
        }

        ShowHome();

        var root = await WaitForXamlRootAsync();
        if (root is null)
        {
            return;
        }

        var dialog = new CompressDialog
        {
            XamlRoot = root,
        };
        dialog.InitializeForInputPaths(inputPaths);

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            var options = dialog.BuildOptions();
            await RunArchiveOperationAsync("압축하는 중", (token, progress) => App.Services.CreateArchive.ExecuteAsync(inputPaths, options, token, progress));
            await App.Services.UserPreferences.SaveLastCompressionOutputDirectoryAsync(
                Path.GetDirectoryName(options.OutputPath));

            var resultPath = string.IsNullOrWhiteSpace(options.SplitSize) ? options.OutputPath : options.OutputPath + ".001";
            if (!ShowArchive(resultPath))
            {
                await ShowErrorDialogAsync(
                    "\uC555\uCD95 \uC644\uB8CC",
                    "\uC555\uCD95 \uD30C\uC77C\uC744 \uB9CC\uB4E4\uC5C8\uC9C0\uB9CC \uACB0\uACFC \uD654\uBA74\uC744 \uC5F4\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.");
            }
        }
        catch (OperationCanceledException)
        {
            await ShowErrorDialogAsync("압축 취소", "작업을 중지했습니다. 생성 중이던 압축 파일은 완성되지 않았을 수 있습니다.");
        }
        catch (Exception ex)
        {
            App.ReportException("MainWindow.ShowCreateArchiveDialogAsync", ex);
            await ShowErrorDialogAsync("\uC555\uCD95 \uC2E4\uD328", ex.Message);
        }
    }

    public async Task HandleIncomingPathsAsync(IReadOnlyList<string> paths)
    {
        var normalizedPaths = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedPaths.Length == 0)
        {
            return;
        }

        if (normalizedPaths.Length == 1 && ArchiveFormatAssetCatalog.IsSupportedOpenArchivePath(normalizedPaths[0]))
        {
            ShowArchive(normalizedPaths[0]);
            return;
        }

        await ShowCreateArchiveDialogAsync(normalizedPaths);
    }

    public void GoBack()
    {
        if (RootFrame.CanGoBack)
        {
            RootFrame.GoBack();
            return;
        }

        if (RootFrame.Content is not HomePage)
        {
            ShowHome();
        }
    }

    public async void ToggleTheme()
    {
        var nextMode = _themeService.Current switch
        {
            Application.Theme.ThemeMode.System => Application.Theme.ThemeMode.Light,
            Application.Theme.ThemeMode.Light => Application.Theme.ThemeMode.Dark,
            _ => Application.Theme.ThemeMode.System,
        };

        await _themeService.ApplyAsync(nextMode);
        _themeService.ApplyTo(this);
    }

    public async Task ApplyThemeAsync(Application.Theme.ThemeMode mode)
    {
        await _themeService.ApplyAsync(mode);
        _themeService.ApplyTo(this);
    }

    public void SetWindowTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "ZipZip" : title;
    }

    private void OnInitialWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialWindowSizeApplied)
        {
            return;
        }

        _initialWindowSizeApplied = true;
        Activated -= OnInitialWindowActivated;
        ApplyInitialWindowSize();
    }

    private void OnRootFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _themeService.ApplyTo(this);
        UpdateWindowTitleForCurrentPage();
        if (e.Content is FrameworkElement page) page.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        var page = (FrameworkElement)sender;
        page.Loaded -= OnPageLoaded;
        // Activation can precede navigation; establish focus once page controls exist.
        (Microsoft.UI.Xaml.Input.FocusManager.FindFirstFocusableElement(page) as Control)
            ?.Focus(FocusState.Programmatic);
    }

    private async void OnFileKeyboardAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (sender.Key == Windows.System.VirtualKey.O) await OpenArchivePickerAsync();
        else await PickInputsAndCreateArchiveAsync();
    }

    private async void OnOpenArchiveMenuClick(object sender, RoutedEventArgs e)
    {
        await OpenArchivePickerAsync();
    }

    private async void OnCreateArchiveMenuClick(object sender, RoutedEventArgs e)
    {
        await PickInputsAndCreateArchiveAsync();
    }

    public async Task RunArchiveOperationAsync(string title, Func<CancellationToken, IProgress<int>, Task> operation)
    {
        var root = await WaitForXamlRootAsync() ?? throw new InvalidOperationException("작업 창을 열 수 없습니다.");
        using var cancellation = new CancellationTokenSource();
        var bar = new ProgressBar { IsIndeterminate = true, MinWidth = 280 };
        var status = new TextBlock { Text = "준비 중…", TextWrapping = TextWrapping.Wrap };
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var active = true;
        var updates = new Progress<int>(value =>
        {
            if (!active || cancellation.IsCancellationRequested) return;
            bar.IsIndeterminate = false;
            bar.Value = value;
            status.Text = $"{value}% · 경과 {elapsed.Elapsed:mm\\:ss}";
        });
        var progress = new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            CloseButtonText = "취소",
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    bar,
                    status,
                },
            },
        };
        progress.CloseButtonClick += (_, args) =>
        {
            args.Cancel = true;
            progress.Title = "작업을 중지하는 중";
            cancellation.Cancel();
        };
        var showing = progress.ShowAsync();
        try { await operation(cancellation.Token, updates); }
        finally
        {
            active = false;
            progress.Hide();
            await showing;
        }
    }

    private async void OnCreateFolderMenuClick(object sender, RoutedEventArgs e)
    {
        await PickFolderAndCreateArchiveAsync();
    }

    private void OnSettingsMenuClick(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void OnExitMenuClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnShowHomeMenuClick(object sender, RoutedEventArgs e)
    {
        ShowHome();
    }

    private void OnGoBackMenuClick(object sender, RoutedEventArgs e)
    {
        GoBack();
    }

    private void OnToggleThemeMenuClick(object sender, RoutedEventArgs e)
    {
        ToggleTheme();
    }

    private async Task<XamlRoot?> WaitForXamlRootAsync()
    {
        for (var index = 0; index < 20; index++)
        {
            if (RootFrame.Content is FrameworkElement element && element.XamlRoot is not null)
            {
                return element.XamlRoot;
            }

            await Task.Delay(50);
        }

        return null;
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var root = await WaitForXamlRootAsync();
        if (root is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            Content = string.IsNullOrWhiteSpace(message) ? "\uC791\uC5C5\uC744 \uC644\uB8CC\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4." : message,
            CloseButtonText = "\uD655\uC778",
            DefaultButton = ContentDialogButton.Close,
        };

        await dialog.ShowAsync();
    }

    private void ApplyInitialWindowSize()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            var targetWidth = Math.Min(1120, workArea.Width - 48);
            var targetHeight = Math.Min(760, workArea.Height - 48);
            appWindow.MoveAndResize(new RectInt32(
                workArea.X + (workArea.Width - targetWidth) / 2,
                workArea.Y + (workArea.Height - targetHeight) / 2,
                targetWidth, targetHeight));
        }
        catch
        {
        }
    }

    private void UpdateWindowTitleForCurrentPage()
    {
        switch (RootFrame.Content)
        {
            case HomePage:
                SetWindowTitle("ZipZip");
                break;
            case SettingsPage:
                SetWindowTitle("\uC124\uC815 - ZipZip");
                break;
        }
    }

    private void ApplyWindowIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "App", "ZipZip.ico");
            if (!File.Exists(iconPath))
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(iconPath);
        }
        catch
        {
        }
    }
}
