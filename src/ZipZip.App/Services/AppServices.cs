using ZipZip.Application.UseCases;
using ZipZip.ArchiveAdapters.SevenZip;

namespace ZipZip.App.Services;

public sealed class AppServices
{
    public AppServices()
    {
        var backend = new SevenZipArchiveBackend();
        UserPreferences = new UserPreferencesService();
        FileAssociations = new FileAssociationService();
        ExplorerMenus = new ExplorerMenuService();
        Theme = new ThemeService(UserPreferences);
        OpenArchive = new OpenArchiveUseCase(backend);
        CreateArchive = new CreateArchiveUseCase(backend);
        ExtractArchive = new ExtractArchiveUseCase(backend);
    }

    public UserPreferencesService UserPreferences { get; }

    public FileAssociationService FileAssociations { get; }

    public ExplorerMenuService ExplorerMenus { get; }

    public ThemeService Theme { get; }

    public OpenArchiveUseCase OpenArchive { get; }

    public CreateArchiveUseCase CreateArchive { get; }

    public ExtractArchiveUseCase ExtractArchive { get; }
}
