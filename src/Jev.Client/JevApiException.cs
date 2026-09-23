using System.Net;

namespace Jev.Client;

/// <summary>Represents a non-successful response from the TypeSafe API.</summary>
public sealed class JevApiException : HttpRequestException
{
    internal JevApiException(HttpStatusCode statusCode, string responseBody)
        : base($"TypeSafe API returned HTTP {(int)statusCode} ({statusCode}).", null, statusCode)
    {
        ResponseBody = responseBody;
    }

    /// <summary>Gets the response body returned by the API.</summary>
    public string ResponseBody { get; }
}
