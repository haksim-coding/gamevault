using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace GameVault.Services;

public class CatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CatalogGameDto>> GetCatalogAsync(
        string manifestUrl,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(manifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var games = await JsonSerializer.DeserializeAsync<List<CatalogGameDto>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return games?
            .Where(game => !string.IsNullOrWhiteSpace(game.Id)
                           && !string.IsNullOrWhiteSpace(game.Title)
                           && !string.IsNullOrWhiteSpace(game.DownloadUrl))
            .ToList() ?? [];
    }

    public async Task<byte[]> GetCoverBytesAsync(
        CatalogGameDto catalogGame,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(catalogGame.CachedCoverPath)
            && File.Exists(catalogGame.CachedCoverPath))
        {
            return await File.ReadAllBytesAsync(catalogGame.CachedCoverPath, cancellationToken);
        }

        using var response = await _httpClient.GetAsync(catalogGame.ResolvedCoverUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(catalogGame.CachedCoverPath))
        {
            Directory.CreateDirectory(AppSettings.CoverCacheFolder);
            await File.WriteAllBytesAsync(catalogGame.CachedCoverPath, bytes, cancellationToken);
        }

        return bytes;
    }
}
