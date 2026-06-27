using System.Net.Http;
using System.Text.Json;

namespace GameVault.Services;

public interface IExternalRestService
{
    Task<string?> GetPublicIpAddressAsync(CancellationToken cancellationToken = default);
}

public sealed class ExternalRestService : IExternalRestService
{
    private static readonly Uri PublicIpEndpoint =
        new("https://api.ipify.org?format=json");

    private readonly HttpClient _httpClient;

    public ExternalRestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetPublicIpAddressAsync(
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        using var response = await _httpClient.GetAsync(
            PublicIpEndpoint,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(timeout.Token);
        var publicIp = await JsonSerializer.DeserializeAsync<PublicIpResponse>(
            content,
            cancellationToken: timeout.Token);

        return string.IsNullOrWhiteSpace(publicIp?.Ip)
            ? null
            : publicIp.Ip.Trim();
    }
}
