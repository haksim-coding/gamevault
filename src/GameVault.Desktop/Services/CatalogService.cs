using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO;

namespace GameVault.Services;

public class CatalogService
{
    private readonly HttpClient _httpClient;
    private readonly string _publicKeyPath;

    public CatalogService(HttpClient httpClient, string? publicKeyPath = null)
    {
        _httpClient = httpClient;
        _publicKeyPath = publicKeyPath ?? Path.Combine(AppContext.BaseDirectory, "public_key.pem");
    }

    public async Task<List<CatalogGameDto>> GetCatalogAsync(
        string manifestUrl,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_publicKeyPath))
        {
            throw new CatalogSignatureException(
                CatalogSignatureFailure.PublicKeyMissing,
                "Manifest public key was not found.");
        }

        var manifestBytes = await DownloadManifestBytesAsync(manifestUrl, cancellationToken);
        var signatureBytes = await DownloadSignatureBytesAsync(manifestUrl + ".sig", cancellationToken);
        VerifySignature(manifestBytes, signatureBytes);

        var games = JsonSerializer.Deserialize<List<CatalogGameDto>>(
            manifestBytes,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return games?
            .Where(game => !string.IsNullOrWhiteSpace(game.Id)
                           && !string.IsNullOrWhiteSpace(game.Title)
                           && !string.IsNullOrWhiteSpace(game.DownloadUrl))
            .ToList() ?? [];
    }

    private async Task<byte[]> DownloadManifestBytesAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<byte[]> DownloadSignatureBytesAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new CatalogSignatureException(
                CatalogSignatureFailure.SignatureMissing,
                "The server catalog signature could not be found.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private void VerifySignature(byte[] manifestBytes, byte[] signatureBytes)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(_publicKeyPath));
            if (!rsa.VerifyData(
                    manifestBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1))
            {
                throw new CatalogSignatureException(
                    CatalogSignatureFailure.InvalidSignature,
                    "The server catalog signature is invalid. Catalog cannot be trusted.");
            }
        }
        catch (CatalogSignatureException)
        {
            throw;
        }
        catch (Exception exception) when (exception is CryptographicException or IOException)
        {
            throw new CatalogSignatureException(
                CatalogSignatureFailure.VerificationFailed,
                "Catalog signature verification failed.",
                exception);
        }
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
