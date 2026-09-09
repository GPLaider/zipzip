using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZipZip.Application.UseCases;
using ZipZip.App.Visuals;
using ZipZip.Domain.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;

namespace ZipZip.App.ViewModels;

public sealed class ArchiveViewModel : ObservableObject
{
    private readonly List<ArchiveEntry> _allEntries = [];
    private IReadOnlyList<ArchiveEntryItemViewModel> _entries = [];
    private string _searchText = string.Empty;
    private int _sortIndex;
    private string _archiveName = string.Empty;
    private string _archivePath = string.Empty;
    private string _currentFolderPath = string.Empty;
    private string _formatLabel = "ZIP";
    private ImageSource _formatIconSource = ArchiveFormatAssetCatalog.GetImageSource("zip");
    private bool _isLoading;
    private string? _errorMessage;
    private string? _operationMessage;
    private InfoBarSeverity _operationSeverity = InfoBarSeverity.Informational;
    private int _selectedCount;
    private long _selectedOriginalSize;

    public ArchiveViewModel()
    {
        OpenCompletedFolderCommand = new RelayCommand(OpenCompletedFolder, () => HasCompletedFolderPath);
    }
    private string? _completedFolderPath;

    public string ArchiveName
    {
        get => _archiveName;
        private set => SetProperty(ref _archiveName, value);
    }

    public string ArchivePath
    {
        get => _archivePath;
        private set
        {
            if (SetProperty(ref _archivePath, value))
            {
                OnPropertyChanged(nameof(CurrentLocationText));
                OnPropertyChanged(nameof(ArchivePathDisplay));
                OnPropertyChanged(nameof(CanExtract));
            }
        }
    }

    public string ArchivePathDisplay => string.IsNullOrWhiteSpace(ArchivePath) ? "-" : ArchivePath;

    public string CurrentFolderPath
    {
        get => _currentFolderPath;
        private set
        {
            if (SetProperty(ref _currentFolderPath, value))
            {
                OnPropertyChanged(nameof(CurrentLocationText));
                OnPropertyChanged(nameof(CanNavigateUpFolder));
                OnPropertyChanged(nameof(CurrentFolderDisplay));
            }
        }
    }

    public string CurrentLocationText => string.IsNullOrWhiteSpace(CurrentFolderPath)
        ? "\uB8E8\uD2B8"
        : $"\uB8E8\uD2B8 > {CurrentFolderPath.Replace("/", " > ")}";

    public string CurrentFolderDisplay => string.IsNullOrWhiteSpace(CurrentFolderPath) ? "\uB8E8\uD2B8" : CurrentFolderPath;

    public bool CanNavigateUpFolder => !string.IsNullOrWhiteSpace(CurrentFolderPath);

    public string FormatLabel
    {
        get => _formatLabel;
        private set => SetProperty(ref _formatLabel, value);
    }

    public ImageSource FormatIconSource
    {
        get => _formatIconSource;
        private set => SetProperty(ref _formatIconSource, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(LoadingVisibility));
                OnPropertyChanged(nameof(EmptyVisibility));
                OnPropertyChanged(nameof(CanExtract));
                OnPropertyChanged(nameof(CanExtractSelected));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ErrorVisibility));
                OnPropertyChanged(nameof(EmptyVisibility));
                OnPropertyChanged(nameof(CanExtract));
                OnPropertyChanged(nameof(CanExtractSelected));
            }
        }
    }

    public string? OperationMessage
    {
        get => _operationMessage;
        private set
        {
            if (SetProperty(ref _operationMessage, value))
            {
                OnPropertyChanged(nameof(HasOperationMessage));
            }
        }
    }

    public InfoBarSeverity OperationSeverity
    {
        get => _operationSeverity;
        private set => SetProperty(ref _operationSeverity, value);
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool CanExtract => !IsLoading && !HasError && !string.IsNullOrWhiteSpace(ArchivePath);
    public bool CanExtractSelected => CanExtract && SelectedCount > 0;

    public bool HasOperationMessage => !string.IsNullOrWhiteSpace(OperationMessage);

    public IRelayCommand OpenCompletedFolderCommand { get; }

    public string? CompletedFolderPath
    {
        get => _completedFolderPath;
        private set
        {
            if (SetProperty(ref _completedFolderPath, value))
            {
                OnPropertyChanged(nameof(HasCompletedFolderPath));
                OnPropertyChanged(nameof(CompletedFolderActionVisibility));
                OpenCompletedFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasCompletedFolderPath => !string.IsNullOrWhiteSpace(CompletedFolderPath);

    public Visibility CompletedFolderActionVisibility => HasCompletedFolderPath ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public string EntrySummary => $"\uD56D\uBAA9 {Entries.Count}\uAC1C";

    public string SelectionSummary => SelectedCount == 0
        ? "\uC120\uD0DD \uC5C6\uC74C"
        : $"\uC120\uD0DD {SelectedCount}\uAC1C {ArchiveEntryItemViewModel.FormatFileSize(SelectedOriginalSize)}";

    public string FooterSummary => $"{EntrySummary} / {SelectionSummary}";

    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(SelectionSummary));
                OnPropertyChanged(nameof(FooterSummary));
                OnPropertyChanged(nameof(CanExtractSelected));
            }
        }
    }

    public long SelectedOriginalSize
    {
        get => _selectedOriginalSize;
        private set
        {
            if (SetProperty(ref _selectedOriginalSize, value))
            {
                OnPropertyChanged(nameof(SelectionSummary));
                OnPropertyChanged(nameof(FooterSummary));
            }
        }
    }

    public IReadOnlyList<ArchiveEntryItemViewModel> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) RefreshEntries(); }
    }

    public int SortIndex
    {
        get => _sortIndex;
        set { if (SetProperty(ref _sortIndex, value)) RefreshEntries(); }
    }

    public Visibility EmptyVisibility => !IsLoading && !HasError && Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchText) ? "이 폴더는 비어 있습니다." : "검색 결과가 없습니다. 검색어를 바꾸거나 지워 주세요.";

    public ObservableCollection<BreadcrumbSegmentViewModel> BreadcrumbSegments { get; } = [];

    public async Task LoadAsync(OpenArchiveUseCase useCase, string archivePath, CancellationToken cancellationToken = default, string? password = null)
    {
        IsLoading = true;
        ErrorMessage = null;
        ClearOperation();
        ArchivePath = archivePath;
        ArchiveName = Path.GetFileName(archivePath);
        FormatLabel = ArchiveFormatAssetCatalog.GetDisplayLabel(archivePath);
        FormatIconSource = ArchiveFormatAssetCatalog.GetImageSource(archivePath);

        try
        {
            var summary = await useCase.ExecuteAsync(archivePath, cancellationToken, password);

            _allEntries.Clear();
            _allEntries.AddRange(summary.Entries);
            NavigateToFolder(string.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Entries = [];
            _allEntries.Clear();
            BreadcrumbSegments.Clear();
            BreadcrumbSegments.Add(new BreadcrumbSegmentViewModel("\uB8E8\uD2B8", string.Empty));
            SelectedCount = 0;
            SelectedOriginalSize = 0;
            OnPropertyChanged(nameof(EntrySummary));
            OnPropertyChanged(nameof(FooterSummary));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        CompletedFolderPath = null;
        ClearOperation();
    }

    public void SetOperationSuccess(string message, string? completedFolderPath = null)
    {
        ErrorMessage = null;
        OperationSeverity = InfoBarSeverity.Success;
        OperationMessage = message;
        CompletedFolderPath = completedFolderPath;
    }

    public void SetOperationInfo(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        ErrorMessage = null;
        OperationSeverity = severity;
        OperationMessage = message;
        CompletedFolderPath = null;
    }

    public void ClearOperation()
    {
        OperationMessage = null;
        CompletedFolderPath = null;
    }

    public void UpdateSelection(IReadOnlyList<ArchiveEntryItemViewModel> selectedItems)
    {
        SelectedCount = selectedItems.Count;
        SelectedOriginalSize = selectedItems.Sum(item => item.OriginalSize);
    }

    public bool NavigateInto(ArchiveEntryItemViewModel item)
    {
        if (!item.IsDirectory)
        {
            return false;
        }

        NavigateToFolder(item.InternalPath);
        return true;
    }

    public bool NavigateUp()
    {
        if (!CanNavigateUpFolder)
        {
            return false;
        }

        NavigateToFolder(GetParentPath(CurrentFolderPath));
        return true;
    }

    public void NavigateToBreadcrumb(string folderPath)
    {
        NavigateToFolder(folderPath);
    }

    private void NavigateToFolder(string folderPath)
    {
        CurrentFolderPath = NormalizePath(folderPath);
        RebuildBreadcrumbs();
        _searchText = string.Empty;
        OnPropertyChanged(nameof(SearchText));
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        Entries = ZipZip.Application.ArchiveEntryQuery.Apply(_allEntries, CurrentFolderPath, SearchText, SortIndex)
            .Select(entry => new ArchiveEntryItemViewModel(entry)).ToArray();
        SelectedCount = 0;
        SelectedOriginalSize = 0;
        OnPropertyChanged(nameof(EntrySummary));
        OnPropertyChanged(nameof(FooterSummary));
        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void RebuildBreadcrumbs()
    {
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new BreadcrumbSegmentViewModel("\uB8E8\uD2B8", string.Empty));

        if (string.IsNullOrWhiteSpace(CurrentFolderPath))
        {
            return;
        }

        var parts = CurrentFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = string.Empty;

        foreach (var part in parts)
        {
            path = string.IsNullOrEmpty(path) ? part : $"{path}/{part}";
            BreadcrumbSegments.Add(new BreadcrumbSegmentViewModel(part, path));
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static string GetParentPath(string path)
    {
        var normalized = NormalizePath(path);
        var separatorIndex = normalized.LastIndexOf('/');
        return separatorIndex < 0 ? string.Empty : normalized[..separatorIndex];
    }

    private void OpenCompletedFolder()
    {
        if (!HasCompletedFolderPath || string.IsNullOrWhiteSpace(CompletedFolderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = CompletedFolderPath,
            UseShellExecute = true,
        });
    }
}

public sealed class BreadcrumbSegmentViewModel
{
    public BreadcrumbSegmentViewModel(string label, string path)
    {
        Label = label;
        Path = path;
    }

    public string Label { get; }

    public string Path { get; }
}

public sealed class ArchiveEntryItemViewModel
{
    public ArchiveEntryItemViewModel(ArchiveEntry entry)
    {
        InternalPath = entry.Path ?? entry.Name;
        Name = entry.Name;
        TypeLabel = string.IsNullOrWhiteSpace(entry.TypeLabel)
            ? ResolveTypeLabel(entry)
            : entry.TypeLabel;
        IsDirectory = entry.IsDirectory;
        OriginalSize = entry.OriginalSize;
        PackedSizeText = FormatFileSize(entry.PackedSize);
        OriginalSizeText = FormatFileSize(entry.OriginalSize);
        IconGlyph = ResolveGlyph(entry);

        var hasArchiveIcon = !entry.IsDirectory && ArchiveFormatAssetCatalog.IsKnownArchiveFormat(entry.Name);
        ArchiveIconSource = hasArchiveIcon ? ArchiveFormatAssetCatalog.GetImageSource(entry.Name) : null;
        ArchiveIconVisibility = hasArchiveIcon ? Visibility.Visible : Visibility.Collapsed;
        GlyphIconVisibility = hasArchiveIcon ? Visibility.Collapsed : Visibility.Visible;

    }

    public string InternalPath { get; }

    public string Name { get; }

    public string TypeLabel { get; }

    public bool IsDirectory { get; }

    public long OriginalSize { get; }

    public string PackedSizeText { get; }

    public string OriginalSizeText { get; }
    public string AccessibleName => $"{Name}, {TypeLabel}, 원본 크기 {OriginalSizeText}, 압축 크기 {PackedSizeText}";

    public string IconGlyph { get; }

    public ImageSource? ArchiveIconSource { get; }

    public Visibility ArchiveIconVisibility { get; }

    public Visibility GlyphIconVisibility { get; }

    private static string ResolveGlyph(ArchiveEntry entry)
    {
        if (entry.IsDirectory)
        {
            return "\uE8B7";
        }

        return Path.GetExtension(entry.Name).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "\uEB9F",
            ".txt" or ".md" or ".rtf" => "\uE8A5",
            ".pdf" => "\uEA90",
            ".exe" => "\uE756",
            _ => "\uE8A5",
        };
    }

    private static string ResolveTypeLabel(ArchiveEntry entry)
    {
        if (entry.IsDirectory)
        {
            return "\uD3F4\uB354";
        }

        if (ArchiveFormatAssetCatalog.IsKnownArchiveFormat(entry.Name))
        {
            return $"{ArchiveFormatAssetCatalog.GetDisplayLabel(entry.Name)} \uC555\uCD95 \uD30C\uC77C";
        }

        var extension = Path.GetExtension(entry.Name).TrimStart('.').ToUpperInvariant();
        return string.IsNullOrWhiteSpace(extension) ? "\uD30C\uC77C" : $"{extension} \uD30C\uC77C";
    }

    internal static string FormatFileSize(long size)
    {
        if (size <= 0)
        {
            return "-";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.0} {units[unitIndex]}";
    }
}
