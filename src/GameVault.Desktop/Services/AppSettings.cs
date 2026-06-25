using System.IO;

namespace GameVault.Services;

public class AppSettings
{
    public const string ApiBaseUrl = "http://192.168.88.110";
    public const string DownloadManifestUrl = "http://192.168.88.110/files/manifests/downloads.json";
    public const string CoverBaseUrl = ApiBaseUrl + "/files/covers";
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
}
