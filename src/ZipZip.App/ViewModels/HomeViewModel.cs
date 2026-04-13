using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ZipZip.App.Visuals;
using Microsoft.UI.Xaml.Media;

namespace ZipZip.App.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private string _recentArchiveCountLabel = "\uCD5C\uADFC 0\uAC1C";

    public ObservableCollection<RecentArchiveItem> RecentArchives { get; } = [];

    public string RecentArchiveCountLabel
    {
        get => _recentArchiveCountLabel;
        private set => SetProperty(ref _recentArchiveCountLabel, value);
    }

    public void LoadRecentArchives(IEnumerable<Services.RecentArchivePreference> recentArchives)
    {
        RecentArchives.Clear();

        foreach (var item in recentArchives)
        {
            RecentArchives.Add(new RecentArchiveItem
            {
                FileName = item.FileName,
                FormatLabel = ArchiveFormatAssetCatalog.GetDisplayLabel(item.FileName),
                LastOpenedLabel = GetRelativeLabel(item.LastOpenedAt),
                FullPath = item.FullPath,
                IconSource = ArchiveFormatAssetCatalog.GetImageSource(item.FileName),
            });
        }

        RecentArchiveCountLabel = $"\uCD5C\uADFC {RecentArchives.Count}\uAC1C";
    }

    private static string GetRelativeLabel(DateTimeOffset lastOpenedAt)
    {
        var now = DateTimeOffset.Now;
        var diff = now - lastOpenedAt;

        if (diff.TotalMinutes < 1)
        {
            return "\uBC29\uAE08 \uC804";
        }

        if (diff.TotalHours < 1)
        {
            return $"{Math.Max(1, (int)diff.TotalMinutes)}\uBD84 \uC804";
        }

        if (lastOpenedAt.Date == now.Date)
        {
            return "\uC624\uB298";
        }

        if (lastOpenedAt.Date == now.Date.AddDays(-1))
        {
            return "\uC5B4\uC81C";
        }

        return lastOpenedAt.ToString("yyyy-MM-dd");
    }
}

public sealed class RecentArchiveItem
{
    public string FileName { get; set; } = string.Empty;

    public string FormatLabel { get; set; } = string.Empty;

    public string LastOpenedLabel { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public ImageSource IconSource { get; set; } = ArchiveFormatAssetCatalog.GetImageSource("default");
}
