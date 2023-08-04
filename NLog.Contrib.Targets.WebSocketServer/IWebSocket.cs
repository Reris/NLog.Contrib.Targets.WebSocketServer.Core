using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Contrib.Targets.WebSocketServer;

public interface IWebSocket : IDisposable
{
    WebSocketCloseStatus? CloseStatus { get; }
    string? CloseStatusDescription { get; }
    Task SendText(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken);
    Task SendBinary(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken);
    Task Send(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancelToken);
    Task Close(WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken cancelToken);
    Task<ReceivedMessage> ReceiveMessage(byte[] buffer, CancellationToken cancelToken);

    public record struct ReceivedMessage(WebSocketMessageType Type, ArraySegment<byte> Message);
}