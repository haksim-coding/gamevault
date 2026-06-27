using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using GameVault.Core.Models;
using GameVault.Data;
using GameVault.Localization;

namespace GameVault.Services;

public class DownloadManagerService
{
    private readonly HttpClient _httpClient;
    private readonly GameRepository _gameRepository;
    private readonly ActivityLogRepository _activityLogRepository;
    private readonly IniSettingsService _settingsService;
    private readonly HashService _hashService;
    private readonly SemaphoreSlim _downloadSemaphore = new(2);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();

    public DownloadManagerService(
        HttpClient httpClient,
        GameRepository gameRepository,
        ActivityLogRepository activityLogRepository,
        IniSettingsService settingsService,
        HashService hashService)
    {
        _httpClient = httpClient;
        _gameRepository = gameRepository;
        _activityLogRepository = activityLogRepository;
        _settingsService = settingsService;
        _hashService = hashService;
    }

    public event Action<DownloadState>? StateChanged;

    public bool IsActive(string catalogGameId) => _activeDownloads.ContainsKey(catalogGameId);

    public async Task<bool> InstallAsync(
        CatalogGameDto manifestItem,
        IProgress<DownloadState>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (IsActive(manifestItem.Id))
        {
            Report(manifestItem, "Already downloading", 0, 0, manifestItem.SizeBytes, 0, "", progress);
            return false;
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!_activeDownloads.TryAdd(manifestItem.Id, linkedCts))
        {
            return false;
        }

        try
        {
            _gameRepository.AddOrUpdateCatalogGame(new Game
            {
                CatalogId = manifestItem.Id,
                Title = manifestItem.Title,
                Genre = manifestItem.Genre,
                ReleaseYear = manifestItem.ReleaseYear,
                Developer = manifestItem.Developer,
                InstallStatus = "Queued",
                DownloadProgress = 0,
                Version = manifestItem.Version,
                DownloadUrl = manifestItem.DownloadUrl,
                CoverImagePath = File.Exists(manifestItem.CachedCoverPath) ? manifestItem.CachedCoverPath : string.Empty,
                DateAdded = DateTime.Now
            });
            Report(manifestItem, "Queued", 0, 0, manifestItem.SizeBytes, 0, "", progress);

            await _downloadSemaphore.WaitAsync(linkedCts.Token);
            try
            {
                await RunInstallAsync(manifestItem, progress, linkedCts.Token);
                return true;
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            CleanupCancelledDownload(manifestItem);
            _gameRepository.DeleteGameByCatalogId(manifestItem.Id);
            _activityLogRepository.AddEntry("Download cancelled", manifestItem.Title);
                Report(manifestItem, "Cancelled", 0, 0, manifestItem.SizeBytes, 0,
                    LocalizationService.Instance.Get("DownloadCancelled"), progress);
            return false;
        }
        catch (Exception exception)
        {
            _gameRepository.UpdateCatalogInstallState(manifestItem.Id, "Failed", 0);
            _activityLogRepository.AddEntry("Download failed", manifestItem.Title);
            Report(manifestItem, "Failed", 0, 0, manifestItem.SizeBytes, 0, exception.Message, progress);
            return false;
        }
        finally
        {
            _activeDownloads.TryRemove(manifestItem.Id, out var cts);
            cts?.Dispose();
        }
    }

    public void Cancel(string catalogGameId)
    {
        if (_activeDownloads.TryGetValue(catalogGameId, out var cts))
        {
            cts.Cancel();
        }
    }

    private async Task RunInstallAsync(
        CatalogGameDto manifestItem,
        IProgress<DownloadState>? progress,
        CancellationToken cancellationToken)
    {
        var settings = _settingsService.Load();
        var defaultInstallFolder = string.IsNullOrWhiteSpace(settings.DefaultInstallFolder)
            ? new AppSettings().DefaultInstallFolder
            : settings.DefaultInstallFolder;

        Directory.CreateDirectory(defaultInstallFolder);
        var downloadsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GameVault",
            "Downloads");
        Directory.CreateDirectory(downloadsFolder);

        var zipPath = Path.Combine(downloadsFolder, $"{manifestItem.Id}.zip");
        var installPath = Path.Combine(defaultInstallFolder, SanitizeFolderName(manifestItem.InstallFolderName));
        var installationCompleted = false;
        var extractionStarted = false;

        try
        {
            _gameRepository.AddOrUpdateCatalogGame(new Game
            {
                CatalogId = manifestItem.Id,
                Title = manifestItem.Title,
                Genre = manifestItem.Genre,
                ReleaseYear = manifestItem.ReleaseYear,
                Developer = manifestItem.Developer,
                InstallStatus = "Queued",
                DownloadProgress = 0,
                Version = manifestItem.Version,
                DownloadUrl = manifestItem.DownloadUrl,
                CoverImagePath = File.Exists(manifestItem.CachedCoverPath) ? manifestItem.CachedCoverPath : string.Empty,
                DateAdded = DateTime.Now
            });

            _activityLogRepository.AddEntry("Download started", manifestItem.Title);
            Report(manifestItem, "Queued", 0, 0, manifestItem.SizeBytes, 0, "", progress);

            await DownloadFileAsync(
                manifestItem,
                zipPath,
                progress,
                cancellationToken);

            Report(manifestItem, "Installing", 100, manifestItem.SizeBytes, manifestItem.SizeBytes, 0, "", progress);
            _gameRepository.UpdateCatalogInstallState(manifestItem.Id, "Installing", 100);

            var actualHash = _hashService.ComputeSha256(zipPath);
            if (!string.IsNullOrWhiteSpace(manifestItem.Sha256)
                && !manifestItem.Sha256.Equals(actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    LocalizationService.Instance.Get("IntegrityCheckFailed"));
            }

            _activityLogRepository.AddEntry("SHA256 verified", manifestItem.Title);

            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
            }

            extractionStarted = true;
            ZipFile.ExtractToDirectory(zipPath, installPath);
            var executablePath = Path.Combine(installPath, manifestItem.ExecutableRelativePath);
            if (!File.Exists(executablePath))
            {
                executablePath = Path.Combine(defaultInstallFolder, manifestItem.ExecutableRelativePath);
            }

            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException(
                LocalizationService.Instance.Get("ManifestExecutableMissing"),
                    executablePath);
            }

            var coverPath = await TryDownloadCoverAsync(manifestItem, cancellationToken);
            _gameRepository.UpdateCatalogInstallState(
                manifestItem.Id,
                "Installed",
                100,
                installPath,
                executablePath,
                coverPath);

            installationCompleted = true;
            _activityLogRepository.AddEntry("Download completed", manifestItem.Title);
            _activityLogRepository.AddEntry("Game installed from server", manifestItem.Title);
            Report(manifestItem, "Installed", 100, manifestItem.SizeBytes, manifestItem.SizeBytes, 0, "", progress);
        }
        finally
        {
            CleanupTemporaryInstallFiles(
                zipPath,
                installPath,
                extractionStarted,
                installationCompleted,
                manifestItem.Title);
        }
    }

    private async Task DownloadFileAsync(
        CatalogGameDto manifestItem,
        string destinationPath,
        IProgress<DownloadState>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            manifestItem.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? manifestItem.SizeBytes;
        var isIndeterminate = totalBytes <= 0;

        await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(destinationPath);

        var buffer = new byte[81920];
        long downloadedBytes = 0;
        long windowBytes = 0;
        var totalStopwatch = Stopwatch.StartNew();
        var windowStopwatch = Stopwatch.StartNew();

        Report(manifestItem, "Downloading", 0, 0, totalBytes, 0, "", progress, isIndeterminate);

        while (true)
        {
            var bytesRead = await httpStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;
            windowBytes += bytesRead;

            var progressPercent = isIndeterminate
                ? 0
                : (int)Math.Min(100, downloadedBytes * 100 / totalBytes);
            var speed = totalStopwatch.Elapsed.TotalSeconds <= 0
                ? 0
                : (long)(downloadedBytes / totalStopwatch.Elapsed.TotalSeconds);

            Report(
                manifestItem,
                "Downloading",
                progressPercent,
                downloadedBytes,
                totalBytes,
                speed,
                "",
                progress,
                isIndeterminate);
            _gameRepository.UpdateCatalogInstallState(manifestItem.Id, "Downloading", progressPercent);

            var speedLimitBytesPerSecond = GetCurrentSpeedLimitBytesPerSecond();
            if (speedLimitBytesPerSecond > 0 && windowBytes >= Math.Max(1, speedLimitBytesPerSecond / 4))
            {
                var expectedMilliseconds = windowBytes * 1000.0 / speedLimitBytesPerSecond;
                var delayMilliseconds = expectedMilliseconds - windowStopwatch.Elapsed.TotalMilliseconds;
                if (delayMilliseconds > 1)
                {
                    await Task.Delay((int)delayMilliseconds, cancellationToken);
                }

                windowBytes = 0;
                windowStopwatch.Restart();
            }
            else if (speedLimitBytesPerSecond == 0 && windowBytes > 0)
            {
                windowBytes = 0;
                windowStopwatch.Restart();
            }
        }
    }

    private long GetCurrentSpeedLimitBytesPerSecond()
    {
        try
        {
            return ParseSpeedLimit(_settingsService.Load().DownloadSpeedLimit);
        }
        catch
        {
            return 0;
        }
    }

    private void CleanupCancelledDownload(CatalogGameDto manifestItem)
    {
        try
        {
            var downloadsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GameVault",
                "Downloads");
            var zipPath = Path.Combine(downloadsFolder, $"{manifestItem.Id}.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
        catch
        {
            // Cancellation cleanup is best-effort; the user-facing result is still cancelled.
        }
    }

    private void CleanupTemporaryInstallFiles(
        string zipPath,
        string installPath,
        bool extractionStarted,
        bool installationCompleted,
        string gameTitle)
    {
        var cleanupFailed = false;
        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
                _activityLogRepository.AddEntry("DownloadZipDeleted", gameTitle);
            }
        }
        catch
        {
            cleanupFailed = true;
        }

        try
        {
            if (extractionStarted && !installationCompleted && Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
            }
        }
        catch
        {
            cleanupFailed = true;
        }

        if (cleanupFailed)
        {
            _activityLogRepository.AddEntry("DownloadTempCleanupFailed", gameTitle);
        }
    }

    private async Task<string> TryDownloadCoverAsync(
        CatalogGameDto manifestItem,
        CancellationToken cancellationToken)
    {
        if (File.Exists(manifestItem.CachedCoverPath))
        {
            return manifestItem.CachedCoverPath;
        }

        var coverUrl = manifestItem.ResolvedCoverUrl;
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            return string.Empty;
        }

        try
        {
            Directory.CreateDirectory(AppSettings.CoverCacheFolder);
            var coverPath = manifestItem.CachedCoverPath;
            var bytes = await _httpClient.GetByteArrayAsync(coverUrl, cancellationToken);
            await File.WriteAllBytesAsync(coverPath, bytes, cancellationToken);
            return coverPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static long ParseSpeedLimit(string speedLimit) =>
        speedLimit switch
        {
            "5 MB/s" => 5L * 1024 * 1024,
            "1 MB/s" => 1024L * 1024,
            "500 KB/s" => 500L * 1024,
            _ => 0
        };

    private static string SanitizeFolderName(string folderName)
    {
        var cleanName = string.IsNullOrWhiteSpace(folderName)
            ? "CatalogGame"
            : folderName;

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            cleanName = cleanName.Replace(invalidCharacter, '-');
        }

        return cleanName;
    }

    private void Report(
        CatalogGameDto manifestItem,
        string status,
        int progressPercent,
        long downloadedBytes,
        long totalBytes,
        long speedBytesPerSecond,
        string errorMessage,
        IProgress<DownloadState>? progress,
        bool isIndeterminate = false)
    {
        var state = new DownloadState
        {
            CatalogGameId = manifestItem.Id,
            Title = manifestItem.Title,
            Status = status,
            ProgressPercent = progressPercent,
            DownloadedBytes = downloadedBytes,
            TotalBytes = totalBytes,
            SpeedBytesPerSecond = speedBytesPerSecond,
            ErrorMessage = errorMessage,
            IsIndeterminate = isIndeterminate
        };

        progress?.Report(state);
        StateChanged?.Invoke(state);
    }
}
