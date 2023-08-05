using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Contrib.Targets.WebSocketServer;

public interface IWebSocket : IDisposable
{
    WebSocketCloseStatus? CloseStatus { get; }
    string? CloseStatusDescription { get; }
    Task SendTextAsync(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken = default);
    Task SendBinaryAsync(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken = default);
    Task SendAsync(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancelToken = default);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken cancelToken = default);
    Task<ReceivedMessage> ReceiveMessageAsync(byte[] buffer, CancellationToken cancelToken = default);

    public record struct ReceivedMessage(WebSocketMessageType Type, ArraySegment<byte> Message);
}
