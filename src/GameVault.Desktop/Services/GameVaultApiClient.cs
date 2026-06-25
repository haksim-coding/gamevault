using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GameVault.Services;

public class GameVaultApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthSession _session;
    private string _baseUrl;

    public GameVaultApiClient(string baseUrl, AuthSession session)
    {
        _baseUrl = NormalizeBaseUrl(baseUrl);
        _session = session;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public string BaseUrl => _baseUrl;

    public void SetBaseUrl(string baseUrl) => _baseUrl = NormalizeBaseUrl(baseUrl);

    public Task<LoginResponse> RegisterAsync(
        string username,
        string password,
        string displayName) =>
        SendAsync<LoginResponse>(
            HttpMethod.Post,
            "api/auth/register",
            new { username, password, displayName },
            requiresAuthentication: false);

    public Task<LoginResponse> LoginAsync(string username, string password) =>
        SendAsync<LoginResponse>(
            HttpMethod.Post,
            "api/auth/login",
            new { username, password },
            requiresAuthentication: false);

    public Task<ProfileResponse> GetMyProfileAsync() =>
        SendAsync<ProfileResponse>(HttpMethod.Get, "api/profile/me");

    public Task<ProfileResponse> UpdateMyProfileAsync(
        string displayName,
        string bio,
        string favoriteGame) =>
        SendAsync<ProfileResponse>(
            HttpMethod.Put,
            "api/profile/me",
            new { displayName, bio, favoriteGame });

    public Task<PublicStats> SyncStatsAsync(UpdateStatsRequest stats) =>
        SendAsync<PublicStats>(HttpMethod.Post, "api/stats/me", stats);

    public Task SendFriendRequestAsync(string username) =>
        SendAsync(HttpMethod.Post, "api/friends/request", new { username });

    public Task<IReadOnlyList<PendingFriendRequest>> GetFriendRequestsAsync() =>
        SendAsync<IReadOnlyList<PendingFriendRequest>>(HttpMethod.Get, "api/friends/requests");

    public Task AcceptFriendRequestAsync(int requestId) =>
        SendAsync(HttpMethod.Post, $"api/friends/accept/{requestId}");

    public Task<IReadOnlyList<FriendInfo>> GetFriendsAsync() =>
        SendAsync<IReadOnlyList<FriendInfo>>(HttpMethod.Get, "api/friends");

    public Task RemoveFriendAsync(int friendshipId) =>
        SendAsync(HttpMethod.Delete, $"api/friends/{friendshipId}");

    public Task<PublicStats> GetUserStatsAsync(string username) =>
        SendAsync<PublicStats>(
            HttpMethod.Get,
            $"api/stats/{Uri.EscapeDataString(username)}");

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        bool requiresAuthentication = true)
    {
        using var response = await SendRequestAsync(method, path, body, requiresAuthentication);
        await EnsureSuccessAsync(response);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        bool requiresAuthentication = true)
    {
        using var response = await SendRequestAsync(method, path, body, requiresAuthentication);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new ApiException("The server returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuthentication)
    {
        if (requiresAuthentication && !_session.IsLoggedIn)
        {
            throw new ApiException("Please log in first.", HttpStatusCode.Unauthorized);
        }

        using var request = new HttpRequestMessage(method, new Uri($"{_baseUrl}/{path}"));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (requiresAuthentication)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _session.Token);
        }

        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("GameVault server is unavailable.");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("GameVault server is unavailable.");
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _session.Clear();
        }

        var message = await ReadErrorMessageAsync(response);
        throw new ApiException(message, response.StatusCode);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? $"Server error: {(int)response.StatusCode}.";
            }
        }
        catch (JsonException)
        {
        }

        return response.StatusCode == HttpStatusCode.Unauthorized
            ? "Your session is invalid or has expired. Please log in again."
            : $"Server error: {(int)response.StatusCode} {response.ReasonPhrase}.";
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var value = baseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("API Base URL must be a valid HTTP or HTTPS URL.");
        }

        return value;
    }
}
