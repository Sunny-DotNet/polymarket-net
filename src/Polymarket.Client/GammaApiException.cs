using System.Net;

namespace Polymarket.Client;

public sealed class GammaApiException : Exception
{
    public GammaApiException(string message, HttpStatusCode? statusCode = null, string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode? StatusCode { get; }

    public string? ResponseBody { get; }
}
