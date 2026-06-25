using System.Net;

namespace GameVault.Services;

public class ApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
