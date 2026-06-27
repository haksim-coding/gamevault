using System.Text.Json.Serialization;

namespace GameVault.Services;

public sealed class PublicIpResponse
{
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }
}
