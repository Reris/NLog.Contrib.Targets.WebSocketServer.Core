using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static NLog.Contrib.Targets.WebSocketServer.IWebSocket;

namespace NLog.Contrib.Targets.WebSocketServer.Adapters;

public class WebSocketAdapter : IWebSocket
{
    private readonly WebSocket _webSocket;
    private bool _disposed;

    public WebSocketAdapter(WebSocket webSocket)
    {
        this._webSocket = webSocket;
        this.SequenceTask = Task.CompletedTask;
    }

    public Task SequenceTask { get; private set; }
    public WebSocketCloseStatus? CloseStatus => this._webSocket.CloseStatus;
    public string? CloseStatusDescription => this._webSocket.CloseStatusDescription;

    public Task SendText(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        => this.Send(data, WebSocketMessageType.Text, endOfMessage, cancelToken);

    public Task SendBinary(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        => this.Send(data, WebSocketMessageType.Binary, endOfMessage, cancelToken);

    public Task Send(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancelToken)
    {
        var sendContext = new MessageChunk(data, endOfMessage, messageType, cancelToken);

        return this.SequenceTask = SequencedSend(this, sendContext);

        static async Task SequencedSend(WebSocketAdapter adapter, MessageChunk msg)
        {
            try
            {
                await adapter.SequenceTask.ConfigureAwait(false);
                await adapter._webSocket.SendAsync(msg.Buffer, msg.Type, msg.EndOfMessage, msg.CancelToken).ConfigureAwait(false);
            }
            catch
            {
                // munch
            }
        }
    }

    public Task Close(WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken cancelToken)
        => this._webSocket.CloseAsync(closeStatus, closeDescription, cancelToken);

    public async Task<ReceivedMessage> ReceiveMessage(byte[] buffer, CancellationToken cancelToken)
    {
        var count = 0;
        WebSocketReceiveResult result;
        do
        {
            var segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
            result = await this._webSocket.ReceiveAsync(segment, cancelToken);

            count += result.Count;
        } while (!result.EndOfMessage);

        return new ReceivedMessage(result.MessageType, new ArraySegment<byte>(buffer, 0, count));
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~WebSocketAdapter()
    {
        this.Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        if (disposing)
        {
            this._webSocket.Dispose();
        }
    }

    public readonly struct MessageChunk
    {
        public readonly ArraySegment<byte> Buffer;
        public readonly CancellationToken CancelToken;
        public readonly bool EndOfMessage;
        public readonly WebSocketMessageType Type;

        public MessageChunk(ArraySegment<byte> buffer, bool endOfMessage, WebSocketMessageType type, CancellationToken cancelToken)
        {
            this.Buffer = buffer;
            this.EndOfMessage = endOfMessage;
            this.Type = type;
            this.CancelToken = cancelToken;
        }
    }
}