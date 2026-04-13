namespace ZipZip.Application.Theme;

public interface IThemePreferenceStore
{
    Task<ThemeMode> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ThemeMode mode, CancellationToken cancellationToken = default);
}
