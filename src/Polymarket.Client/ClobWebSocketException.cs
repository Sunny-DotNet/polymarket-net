using System.Net.WebSockets;

namespace Polymarket.Client;

public sealed class ClobWebSocketException : Exception
{
    public ClobWebSocketException(
        string message,
        WebSocketCloseStatus? closeStatus = null,
        string? closeStatusDescription = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        CloseStatus = closeStatus;
        CloseStatusDescription = closeStatusDescription;
    }

    public WebSocketCloseStatus? CloseStatus { get; }

    public string? CloseStatusDescription { get; }
}
