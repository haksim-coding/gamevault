using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string DiscoveryRequest = "GAMEVAULT_DISCOVER_REQUEST";
const string StatusCommand = "GAMEVAULT_STATUS_V1";

var settings = DiscoverySettings.Load();
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine($"GameVault DiscoveryServer starting as '{settings.ServerName}'.");
Console.WriteLine($"UDP discovery: 0.0.0.0:{settings.UdpDiscoveryPort}");
Console.WriteLine($"TCP status: 0.0.0.0:{settings.TcpStatusPort}");
Console.WriteLine($"Advertised IP: {settings.ResolveServerIp()}");

await Task.WhenAll(
    RunUdpDiscoveryAsync(settings, cancellation.Token),
    RunTcpStatusServerAsync(settings, cancellation.Token));

static async Task RunUdpDiscoveryAsync(
    DiscoverySettings settings,
    CancellationToken cancellationToken)
{
    using var udp = new UdpClient(settings.UdpDiscoveryPort)
    {
        EnableBroadcast = true
    };

    while (!cancellationToken.IsCancellationRequested)
    {
        UdpReceiveResult received;
        try
        {
            received = await udp.ReceiveAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"UDP receive failed: {exception.Message}");
            continue;
        }

        var message = Encoding.UTF8.GetString(received.Buffer).Trim();
        if (!message.Equals(DiscoveryRequest, StringComparison.Ordinal))
        {
            continue;
        }

        var response = new DiscoveryResponse(
            settings.ServerName,
            settings.ResolveServerIp(),
            settings.TcpStatusPort);
        var json = JsonSerializer.Serialize(response, JsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);
        await udp.SendAsync(bytes, received.RemoteEndPoint, cancellationToken);
        Console.WriteLine($"Discovery response sent to {received.RemoteEndPoint}.");
    }
}

static async Task RunTcpStatusServerAsync(
    DiscoverySettings settings,
    CancellationToken cancellationToken)
{
    var listener = new TcpListener(IPAddress.Any, settings.TcpStatusPort);
    listener.Start();

    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = Task.Run(
                () => HandleTcpClientAsync(client, settings, cancellationToken),
                cancellationToken);
        }
    }
    finally
    {
        listener.Stop();
    }
}

static async Task HandleTcpClientAsync(
    TcpClient client,
    DiscoverySettings settings,
    CancellationToken cancellationToken)
{
        using var _ = client;
    client.ReceiveTimeout = 5000;
    client.SendTimeout = 5000;

    try
    {
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        var command = (await reader.ReadLineAsync(cancellationToken))?.Trim();
        if (!StatusCommand.Equals(command, StringComparison.Ordinal))
        {
            await writer.WriteLineAsync("{\"status\":\"ERROR\",\"message\":\"Unknown command\"}");
            return;
        }

        var manifestSummary = await ManifestSummary.TryReadAsync(settings.ManifestUrl, cancellationToken);
        var status = new ServerStatusResponse(
            settings.ServerName,
            settings.ServerVersion,
            "OK",
            settings.ApiBaseUrl,
            settings.FilesBaseUrl,
            settings.ManifestUrl,
            manifestSummary.AvailableCatalogItems,
            manifestSummary.CatalogTotalSizeBytes,
            DateTime.UtcNow);
        await writer.WriteLineAsync(JsonSerializer.Serialize(status, JsonOptions.Default));
    }
    catch (Exception exception)
    {
        Console.WriteLine($"TCP client failed: {exception.Message}");
    }
}

public sealed record DiscoverySettings
{
    public string ServerName { get; init; } = "GameVault HomeLab";
    public string ServerIp { get; init; } = string.Empty;
    public int UdpDiscoveryPort { get; init; } = 47891;
    public int TcpStatusPort { get; init; } = 47892;
    public string ApiBaseUrl { get; init; } = "http://192.168.88.110";
    public string FilesBaseUrl { get; init; } = "http://192.168.88.110/files";
    public string ManifestUrl { get; init; } = "http://192.168.88.110/files/manifests/downloads.json";
    public string ServerVersion { get; init; } = "1.0.0";

    public static DiscoverySettings Load()
    {
        var settings = LoadFromJson();
        return settings with
        {
            ServerName = ReadString("SERVERNAME", settings.ServerName),
            ServerIp = ReadString("SERVERIP", settings.ServerIp),
            UdpDiscoveryPort = ReadInt("UDPDISCOVERYPORT", settings.UdpDiscoveryPort),
            TcpStatusPort = ReadInt("TCPSTATUSPORT", settings.TcpStatusPort),
            ApiBaseUrl = ReadString("APIBASEURL", settings.ApiBaseUrl),
            FilesBaseUrl = ReadString("FILESBASEURL", settings.FilesBaseUrl),
            ManifestUrl = ReadString("MANIFESTURL", settings.ManifestUrl),
            ServerVersion = ReadString("SERVERVERSION", settings.ServerVersion)
        };
    }

    public string ResolveServerIp() =>
        string.IsNullOrWhiteSpace(ServerIp) ? FindLocalIpAddress() : ServerIp;

    private static DiscoverySettings LoadFromJson()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (!File.Exists(path))
        {
            return new DiscoverySettings();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.TryGetProperty("GameVaultDiscovery", out var section))
            {
                root = section;
            }

            return JsonSerializer.Deserialize<DiscoverySettings>(
                root.GetRawText(),
                JsonOptions.Default) ?? new DiscoverySettings();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"appsettings.json could not be read: {exception.Message}");
            return new DiscoverySettings();
        }
    }

    private static string ReadString(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable($"GAMEVAULT_DISCOVERY__{key}");
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int ReadInt(string key, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable($"GAMEVAULT_DISCOVERY__{key}"), out var value)
            ? value
            : fallback;

    private static string FindLocalIpAddress()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString();
                }
            }
        }

        return IPAddress.Loopback.ToString();
    }
}

public sealed record DiscoveryResponse(
    string ServerName,
    string ServerIp,
    int TcpStatusPort);

public sealed record ServerStatusResponse(
    string ServerName,
    string ServerVersion,
    string Status,
    string ApiBaseUrl,
    string FilesBaseUrl,
    string ManifestUrl,
    int AvailableCatalogItems,
    long CatalogTotalSizeBytes,
    DateTime ServerTimeUtc);

public sealed record ManifestSummary(int AvailableCatalogItems, long CatalogTotalSizeBytes)
{
    public static async Task<ManifestSummary> TryReadAsync(
        string manifestUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await OpenManifestStreamAsync(manifestUrl, cancellationToken);
            var items = await JsonSerializer.DeserializeAsync<List<ManifestItem>>(
                stream,
                JsonOptions.Default,
                cancellationToken);

            return new ManifestSummary(
                items?.Count ?? 0,
                items?.Sum(item => item.SizeBytes) ?? 0);
        }
        catch
        {
            return new ManifestSummary(0, 0);
        }
    }

    private static async Task<Stream> OpenManifestStreamAsync(
        string manifestUrl,
        CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            return await httpClient.GetStreamAsync(uri, cancellationToken);
        }

        return File.OpenRead(manifestUrl);
    }
}

public sealed class ManifestItem
{
    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
