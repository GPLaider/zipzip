using ZipZip.Application.Theme;

namespace ZipZip.Infrastructure.Settings;

public sealed class InMemoryThemePreferenceStore : IThemePreferenceStore
{
    private ThemeMode _mode = ThemeMode.System;

    public Task<ThemeMode> GetAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_mode);
    }

    public Task SaveAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        _mode = mode;
        return Task.CompletedTask;
    }
}
