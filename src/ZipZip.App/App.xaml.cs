using System.Text;
using ZipZip.App.Activation;
using ZipZip.App.Services;
using Microsoft.UI.Xaml;

namespace ZipZip.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    public static MainWindow? MainWindowInstance { get; private set; }
    public static AppServices Services { get; } = new();
    internal static void ReportException(string source, Exception? exception) => WriteCrashLog(source, exception);

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await Services.Theme.InitializeAsync();

            MainWindowInstance = new MainWindow(Services.Theme);

            MainWindowInstance.Activate();

            var handled = await HandleLaunchCommandAsync(args.Arguments);
            if (!handled)
            {
                MainWindowInstance.ShowHome();
            }
        }
        catch (Exception ex)
        {
            WriteCrashLog("OnLaunched", ex);
            throw;
        }
    }

    private static async Task<bool> HandleLaunchCommandAsync(string? arguments)
    {
        if (MainWindowInstance is null)
        {
            return false;
        }

        var command = LaunchCommandParser.Parse(arguments);
        if (command.Action == LaunchAction.None)
        {
            var fallbackTokens = Environment.GetCommandLineArgs().Skip(1).ToArray();
            command = LaunchCommandParser.Parse(fallbackTokens);
        }

        switch (command.Action)
        {
            case LaunchAction.OpenArchive:
                MainWindowInstance.ShowArchive(command.Paths[0]);
                return true;
            case LaunchAction.CreateArchive:
                await MainWindowInstance.ShowCreateArchiveDialogAsync(command.Paths);
                return true;
            case LaunchAction.ShowSettings:
                MainWindowInstance.ShowSettings();
                return true;
            default:
                return false;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WriteCrashLog("Application.UnhandledException", e.Exception);
    }

    private void OnCurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        WriteCrashLog("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
    }

    private static void WriteCrashLog(string source, Exception? exception)
    {
        try
        {
            var directory = GetLogDirectory();
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "crash.log");
            var builder = new StringBuilder()
                .AppendLine($"[{DateTimeOffset.Now:O}] {source}");

            if (exception is null)
            {
                builder.AppendLine("Exception: <null>");
            }
            else
            {
                builder.AppendLine(exception.ToString());
            }

            builder.AppendLine(new string('-', 60));
            File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZipZip");
    }
}
