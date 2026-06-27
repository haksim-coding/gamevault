namespace GameVault.Localization;

public static class ApiErrorLocalizer
{
    private static readonly Dictionary<string, string> ErrorKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username is already taken."] = "ApiUsernameTaken",
            ["Invalid username or password."] = "ApiInvalidCredentials",
            ["User was not found."] = "ApiUserNotFound",
            ["You cannot send a friend request to yourself."] = "ApiCannotFriendSelf",
            ["A friendship or friend request already exists between these users."] = "ApiFriendshipExists",
            ["Pending friend request was not found."] = "ApiPendingRequestNotFound",
            ["Friendship was not found."] = "ApiFriendshipNotFound"
        };

    public static string Get(string message) =>
        ErrorKeys.TryGetValue(message, out var key)
            ? LocalizationService.Instance.Get(key)
            : LocalizationService.Instance.Get("OperationCouldNotComplete");
}
