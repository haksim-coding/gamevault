using System.IO;
using System.Text.Json.Serialization;

namespace GameVault.Services;

public class CatalogGameDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("genre")]
    public string Genre { get; set; } = string.Empty;

    [JsonPropertyName("developer")]
    public string Developer { get; set; } = string.Empty;

    [JsonPropertyName("releaseYear")]
    public int ReleaseYear { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("coverUrl")]
    public string CoverUrl { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("installFolderName")]
    public string InstallFolderName { get; set; } = string.Empty;

    [JsonPropertyName("executableRelativePath")]
    public string ExecutableRelativePath { get; set; } = string.Empty;

    public string ResolvedCoverUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Id))
            {
                return $"{AppSettings.CoverBaseUrl}/{Uri.EscapeDataString(Id)}.png";
            }

            if (Uri.TryCreate(CoverUrl, UriKind.Absolute, out _))
            {
                return CoverUrl;
            }

            if (!string.IsNullOrWhiteSpace(CoverUrl))
            {
                var coverPath = CoverUrl.TrimStart('/');
                return $"{AppSettings.ApiBaseUrl.TrimEnd('/')}/{coverPath}";
            }

            return string.Empty;
        }
    }

    public string CachedCoverPath =>
        string.IsNullOrWhiteSpace(Id)
            ? string.Empty
            : Path.Combine(AppSettings.CoverCacheFolder, $"{Id}.png");
}
