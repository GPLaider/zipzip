using ZipZip.Application.Theme;

namespace ZipZip.Application.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public async Task SetCurrentAsync_Saves_Theme_Mode()
    {
        var store = new FakeThemePreferenceStore();
        var service = new ThemeService(store);

        await service.SetCurrentAsync(ThemeMode.Dark);

        var current = await service.GetCurrentAsync();
        Assert.Equal(ThemeMode.Dark, current);
    }

    private sealed class FakeThemePreferenceStore : IThemePreferenceStore
    {
        private ThemeMode _current = ThemeMode.System;

        public Task<ThemeMode> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_current);
        }

        public Task SaveAsync(ThemeMode mode, CancellationToken cancellationToken = default)
        {
            _current = mode;
            return Task.CompletedTask;
        }
    }
}
