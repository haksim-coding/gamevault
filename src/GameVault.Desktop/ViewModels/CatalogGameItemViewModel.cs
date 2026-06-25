using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameVault.Services;

namespace GameVault.ViewModels;

public class CatalogGameItemViewModel : ViewModelBase
{
    private string _status = "Not installed";
    private int _progressPercent;
    private long _downloadedBytes;
    private long _totalBytes;
    private long _speedBytesPerSecond;
    private string _errorMessage = string.Empty;
    private bool _isIndeterminate;
    private ImageSource? _coverImage;

    public CatalogGameItemViewModel(CatalogGameDto manifestItem)
    {
        ManifestItem = manifestItem;
        _totalBytes = manifestItem.SizeBytes;
    }

    public CatalogGameDto ManifestItem { get; }
    public string Id => ManifestItem.Id;
    public string Title => ManifestItem.Title;
    public string Genre => ManifestItem.Genre;
    public string Developer => ManifestItem.Developer;
    public string Version => ManifestItem.Version;
    public string Description => ManifestItem.Description;
    public string CoverUrl => ManifestItem.ResolvedCoverUrl;
    public ImageSource? CoverImage
    {
        get => _coverImage;
        private set => SetProperty(ref _coverImage, value);
    }
    public string SizeText => FormatBytes(ManifestItem.SizeBytes);
    public bool IsInstalled => Status.Equals("Installed", StringComparison.OrdinalIgnoreCase);
    public bool IsBusy => Status is "Queued" or "Downloading" or "Installing" or "Already downloading";
    public string PrimaryButtonText => IsInstalled || IsBusy ? "In Library" : "Get";
    public bool CanInstall => !IsInstalled && !IsBusy;
    public bool CanUsePrimaryButton => true;
    public bool CanCancel => Status is "Queued" or "Downloading";

    public string Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsInstalled));
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(PrimaryButtonText));
                OnPropertyChanged(nameof(CanInstall));
                OnPropertyChanged(nameof(CanUsePrimaryButton));
                OnPropertyChanged(nameof(CanCancel));
            }
        }
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        private set => SetProperty(ref _progressPercent, value);
    }

    public long DownloadedBytes
    {
        get => _downloadedBytes;
        private set
        {
            if (SetProperty(ref _downloadedBytes, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public long TotalBytes
    {
        get => _totalBytes;
        private set
        {
            if (SetProperty(ref _totalBytes, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public long SpeedBytesPerSecond
    {
        get => _speedBytesPerSecond;
        private set
        {
            if (SetProperty(ref _speedBytesPerSecond, value))
            {
                OnPropertyChanged(nameof(SpeedText));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set => SetProperty(ref _isIndeterminate, value);
    }

    public string ProgressText =>
        TotalBytes > 0
            ? $"{FormatBytes(DownloadedBytes)} / {FormatBytes(TotalBytes)}"
            : FormatBytes(DownloadedBytes);

    public string SpeedText =>
        SpeedBytesPerSecond > 0
            ? $"{FormatBytes(SpeedBytesPerSecond)}/s"
            : string.Empty;

    public void ApplyGameStatus(string status, int progress)
    {
        Status = string.IsNullOrWhiteSpace(status) ? "Not installed" : status;
        ProgressPercent = progress;
    }

    public void ApplyDownloadState(DownloadState state)
    {
        Status = state.Status;
        ProgressPercent = state.ProgressPercent;
        DownloadedBytes = state.DownloadedBytes;
        TotalBytes = state.TotalBytes;
        SpeedBytesPerSecond = state.SpeedBytesPerSecond;
        ErrorMessage = state.ErrorMessage;
        IsIndeterminate = state.IsIndeterminate;
    }

    public async Task<bool> LoadCoverAsync(
        CatalogService catalogService,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(CoverUrl))
        {
            return false;
        }

        try
        {
            var bytes = await catalogService.GetCoverBytesAsync(ManifestItem, cancellationToken);
            CoverImage = CreateCoverImage(bytes);
            return CoverImage is not null;
        }
        catch
        {
            CoverImage = null;
            return false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "Unknown size";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }

    private static BitmapImage? CreateCoverImage(byte[] imageBytes)
    {
        if (imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(imageBytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
