namespace ZipZip.Application.Theme;

public sealed class ThemeService
{
    private readonly IThemePreferenceStore _store;

    public ThemeService(IThemePreferenceStore store)
    {
        _store = store;
    }

    public Task<ThemeMode> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(cancellationToken);
    }

    public Task SetCurrentAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        return _store.SaveAsync(mode, cancellationToken);
    }
}
