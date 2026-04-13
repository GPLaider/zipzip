using Windows.ApplicationModel.DataTransfer;

namespace ZipZip.App.Services;

public static class IncomingItemService
{
    public static bool CanAccept(DataPackageView dataView)
    {
        return dataView.Contains(StandardDataFormats.StorageItems);
    }

    public static async Task<IReadOnlyList<string>> GetPathsAsync(DataPackageView dataView)
    {
        if (!CanAccept(dataView))
        {
            return [];
        }

        var storageItems = await dataView.GetStorageItemsAsync();
        return storageItems
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
