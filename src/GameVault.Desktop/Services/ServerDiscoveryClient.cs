using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using GameVault.Data;
using GameVault.Localization;

namespace GameVault.Services;

public interface IServerDiscoveryClient
{
    Task<DiscoveredServer?> DiscoverServerAsync(CancellationToken cancellationToken = default);
    Task<GameVaultServerStatus?> GetServerStatusAsync(
        DiscoveredServer discoveredServer,
        CancellationToken cancellationToken = default);
    Task<ServerDiscoveryApplyResult> FindAndApplyServerSettingsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class ServerDiscoveryClient : IServerDiscoveryClient
{
    private const string DiscoveryRequest = "GAMEVAULT_DISCOVER_REQUEST";
    private const string StatusCommand = "GAMEVAULT_STATUS_V1";
    private const int UdpDiscoveryPort = 47891;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IniSettingsService _settingsService;
    private readonly ActivityLogRepository _activityLogRepository;

    public ServerDiscoveryClient(
        IniSettingsService settingsService,
        ActivityLogRepository activityLogRepository)
    {
        _settingsService = settingsService;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<DiscoveredServer?> DiscoverServerAsync(
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));

        using var udp = new UdpClient
        {
            EnableBroadcast = true
        };

        var requestBytes = Encoding.UTF8.GetBytes(DiscoveryRequest);
        await udp.SendAsync(
            requestBytes,
            new IPEndPoint(IPAddress.Parse("192.168.88.255"), UdpDiscoveryPort),
            timeout.Token);

        var response = await udp.ReceiveAsync(timeout.Token);
        var json = Encoding.UTF8.GetString(response.Buffer);
        var discovered = JsonSerializer.Deserialize<DiscoveredServer>(json, JsonOptions);
        if (discovered is null ||
            string.IsNullOrWhiteSpace(discovered.ServerName) ||
            string.IsNullOrWhiteSpace(discovered.ServerIp) ||
            discovered.TcpStatusPort <= 0)
        {
            return null;
        }

        return discovered;
    }

    public async Task<GameVaultServerStatus?> GetServerStatusAsync(
        DiscoveredServer discoveredServer,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(discoveredServer.ServerIp, discoveredServer.TcpStatusPort, timeout.Token);
        await using var stream = tcp.GetStream();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(StatusCommand);
        var json = await reader.ReadLineAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var status = JsonSerializer.Deserialize<GameVaultServerStatus>(json, JsonOptions);
        if (status is null ||
            !status.Status.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(status.ApiBaseUrl) ||
            string.IsNullOrWhiteSpace(status.ManifestUrl) ||
            !IsHttpUrl(status.ApiBaseUrl) ||
            !IsHttpUrl(status.ManifestUrl) ||
            (!string.IsNullOrWhiteSpace(status.FilesBaseUrl) && !IsHttpUrl(status.FilesBaseUrl)))
        {
            return null;
        }

        return status;
    }

    public async Task<ServerDiscoveryApplyResult> FindAndApplyServerSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            TryLogActivity("ServerDiscoveryStarted");
            var discovered = await DiscoverServerAsync(cancellationToken);
            if (discovered is null)
            {
                TryLogActivity("ServerDiscoveryFailed");
                return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("GameVaultServerUnavailable"));
            }

            var status = await GetServerStatusAsync(discovered, cancellationToken);
            if (status is null)
            {
                TryLogActivity("ServerDiscoveryFailed");
                return ServerDiscoveryApplyResult.Fail(
                    $"Server found: {discovered.ServerName}, but TCP status/config failed.",
                    discovered);
            }

            var settings = _settingsService.Load();
            settings.ApiBaseUrl = status.ApiBaseUrl.TrimEnd('/');
            settings.FilesBaseUrl = string.IsNullOrWhiteSpace(status.FilesBaseUrl)
                ? settings.FilesBaseUrl
                : status.FilesBaseUrl.TrimEnd('/');
            settings.DownloadManifestUrl = status.ManifestUrl;
            _settingsService.Save(settings);

            TryLogActivity("ServerDiscoverySucceeded");
            TryLogActivity("ServerSettingsUpdated");
            return ServerDiscoveryApplyResult.Success(discovered, status);
        }
        catch (JsonException)
        {
            TryLogActivity("ServerDiscoveryFailed");
            return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("InvalidDiscoveryResponse"));
        }
        catch (OperationCanceledException)
        {
            TryLogActivity("ServerDiscoveryFailed");
            return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("GameVaultServerUnavailable"));
        }
        catch (SocketException)
        {
            TryLogActivity("ServerDiscoveryFailed");
            return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("NetworkSearchError"));
        }
        catch (IOException)
        {
            TryLogActivity("ServerDiscoveryFailed");
            return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("NetworkStatusError"));
        }
        catch
        {
            TryLogActivity("ServerDiscoveryFailed");
            return ServerDiscoveryApplyResult.Fail(LocalizationService.Instance.Get("GameVaultServerUnavailable"));
        }
    }

    private void TryLogActivity(string action)
    {
        try
        {
            _activityLogRepository.AddEntry(action);
        }
        catch
        {
            // Discovery should still work if the local activity database is busy.
        }
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class DiscoveredServer
{
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("serverIp")]
    public string ServerIp { get; set; } = string.Empty;

    [JsonPropertyName("tcpStatusPort")]
    public int TcpStatusPort { get; set; }
}

public sealed class GameVaultServerStatus
{
    [JsonPropertyName("serverName")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("serverVersion")]
    public string ServerVersion { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("apiBaseUrl")]
    public string ApiBaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("filesBaseUrl")]
    public string FilesBaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("manifestUrl")]
    public string ManifestUrl { get; set; } = string.Empty;

    [JsonPropertyName("availableCatalogItems")]
    public int AvailableCatalogItems { get; set; }

    [JsonPropertyName("catalogTotalSizeBytes")]
    public long CatalogTotalSizeBytes { get; set; }

    [JsonPropertyName("serverTimeUtc")]
    public DateTime ServerTimeUtc { get; set; }
}

public sealed class ServerDiscoveryApplyResult
{
    private ServerDiscoveryApplyResult(
        bool succeeded,
        string message,
        DiscoveredServer? discoveredServer,
        GameVaultServerStatus? serverStatus)
    {
        Succeeded = succeeded;
        Message = message;
        DiscoveredServer = discoveredServer;
        ServerStatus = serverStatus;
    }

    public bool Succeeded { get; }
    public string Message { get; }
    public DiscoveredServer? DiscoveredServer { get; }
    public GameVaultServerStatus? ServerStatus { get; }

    public static ServerDiscoveryApplyResult Success(
        DiscoveredServer discoveredServer,
        GameVaultServerStatus serverStatus) =>
        new(
            true,
            $"Server settings updated: {serverStatus.ServerName}",
            discoveredServer,
            serverStatus);

    public static ServerDiscoveryApplyResult Fail(
        string message,
        DiscoveredServer? discoveredServer = null) =>
        new(false, message, discoveredServer, null);
}
