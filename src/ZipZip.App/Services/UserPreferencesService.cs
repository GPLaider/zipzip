using System.Text.Json;
using ZipZip.Application.Theme;
using ZipZip.Domain.Models;

namespace ZipZip.App.Services;

public sealed class UserPreferencesService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _preferencesPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UserPreferences? _current;

    public UserPreferencesService()
    {
        var appDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZipZip");

        Directory.CreateDirectory(appDirectory);
        _preferencesPath = Path.Combine(appDirectory, "preferences.json");
    }

    public UserPreferences Current => _current ?? UserPreferences.Default;

    public async Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_current is not null)
            {
                return _current;
            }

            if (!File.Exists(_preferencesPath))
            {
                _current = UserPreferences.Default;
                return _current;
            }

            try
            {
                await using var stream = File.OpenRead(_preferencesPath);
                _current = await JsonSerializer.DeserializeAsync<UserPreferences>(
                               stream,
                               SerializerOptions,
                               cancellationToken)
                           ?? UserPreferences.Default;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                BackupCorruptedPreferences();
                _current = UserPreferences.Default;
                await SaveAsyncUnsafe(_current, cancellationToken);
            }

            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await SaveAsyncUnsafe(preferences, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        await SaveAsync(current with { ThemeMode = mode }, cancellationToken);
    }

    public async Task SaveCompressionDefaultsAsync(
        ArchiveFormat format,
        CompressionLevel level,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        await SaveAsync(current with
        {
            DefaultArchiveFormat = format,
            DefaultCompressionLevel = level,
        }, cancellationToken);
    }

    public async Task SaveLastCompressionOutputDirectoryAsync(
        string? outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var normalizedDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? null
            : outputDirectory.Trim();

        var current = await GetAsync(cancellationToken);
        await SaveAsync(current with
        {
            LastCompressionOutputDirectory = normalizedDirectory,
        }, cancellationToken);
    }

    public async Task AddRecentArchiveAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        var fileName = Path.GetFileName(archivePath);

        var updated = current.RecentArchives
            .Where(item => !string.Equals(item.FullPath, archivePath, StringComparison.OrdinalIgnoreCase))
            .Prepend(new RecentArchivePreference(fileName, archivePath, DateTimeOffset.Now))
            .Take(8)
            .ToArray();

        await SaveAsync(current with { RecentArchives = updated }, cancellationToken);
    }

    public async Task RemoveRecentArchiveAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        var updated = current.RecentArchives
            .Where(item => !string.Equals(item.FullPath, archivePath, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        await SaveAsync(current with { RecentArchives = updated }, cancellationToken);
    }

    public async Task SaveAssociatedArchiveExtensionsAsync(
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken = default)
    {
        var normalized = extensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var current = await GetAsync(cancellationToken);
        await SaveAsync(current with
        {
            AssociatedArchiveExtensions = normalized,
        }, cancellationToken);
    }

    public async Task SaveShellMenuPreferencesAsync(
        ShellMenuPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        await SaveAsync(current with
        {
            ShellMenus = preferences,
        }, cancellationToken);
    }

    private async Task SaveAsyncUnsafe(UserPreferences preferences, CancellationToken cancellationToken)
    {
        _current = preferences;

        var directory = Path.GetDirectoryName(_preferencesPath)
                        ?? throw new InvalidOperationException("\uC124\uC815 \uC800\uC7A5 \uD3F4\uB354\uB97C \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
        Directory.CreateDirectory(directory);

        var temporaryPath = _preferencesPath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, preferences, SerializerOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        if (File.Exists(_preferencesPath))
        {
            File.Copy(temporaryPath, _preferencesPath, overwrite: true);
            File.Delete(temporaryPath);
        }
        else
        {
            File.Move(temporaryPath, _preferencesPath);
        }
    }

    private void BackupCorruptedPreferences()
    {
        try
        {
            if (!File.Exists(_preferencesPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(_preferencesPath)
                            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var backupPath = Path.Combine(
                directory,
                $"preferences.corrupt.{DateTimeOffset.Now:yyyyMMddHHmmss}.json");

            File.Move(_preferencesPath, backupPath, overwrite: true);
        }
        catch
        {
        }
    }
}
