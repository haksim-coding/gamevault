using System.IO;

namespace GameVault.Services;

public class AppSettings
{
    public const string DefaultApiBaseUrl = "http://192.168.88.110";
    public const string DefaultFilesBaseUrl = "http://192.168.88.110/files";
    public const string DefaultDownloadManifestUrl = "http://192.168.88.110/files/manifests/downloads.json";
    public static string CoverCacheFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "GameVault",
        "Covers");

    public string AccentColor { get; set; } = "Purple";
    public string Language { get; set; } = "EN";
    public string DefaultInstallFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "GameVault",
        "InstalledGames");
    public string DownloadSpeedLimit { get; set; } = "Unlimited";
    public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
    public string FilesBaseUrl { get; set; } = DefaultFilesBaseUrl;
    public string DownloadManifestUrl { get; set; } = DefaultDownloadManifestUrl;
}
