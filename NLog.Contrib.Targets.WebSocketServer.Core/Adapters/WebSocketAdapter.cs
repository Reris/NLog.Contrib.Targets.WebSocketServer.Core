using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static NLog.Contrib.Targets.WebSocketServer.Core.IWebSocket;

namespace NLog.Contrib.Targets.WebSocketServer.Core.Adapters;

public class WebSocketAdapter : IWebSocket
{
    private readonly WebSocket _webSocket;
    private bool _disposed;

    public WebSocketAdapter(WebSocket webSocket)
    {
        this._webSocket = webSocket;
        this.SequenceTask = Task.CompletedTask;
    }

    /// <summary>
    /// Finish before send next
    /// </summary>
    private Task SequenceTask { get; set; }

    public WebSocketCloseStatus? CloseStatus => this._webSocket.CloseStatus;
    public string? CloseStatusDescription => this._webSocket.CloseStatusDescription;

    public Task SendTextAsync(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        => this.SendAsync(data, WebSocketMessageType.Text, endOfMessage, cancelToken);

    public Task SendBinaryAsync(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        => this.SendAsync(data, WebSocketMessageType.Binary, endOfMessage, cancelToken);

    public Task SendAsync(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancelToken)
    {
        var sendContext = new MessageChunk(data, endOfMessage, messageType, cancelToken);

        return this.SequenceTask = SequencedSendAsync(this, sendContext);

        static async Task SequencedSendAsync(WebSocketAdapter adapter, MessageChunk msg)
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

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken cancelToken)
        => this._webSocket.CloseAsync(closeStatus, closeDescription, cancelToken);

    public async Task<ReceivedMessage> ReceiveMessageAsync(byte[] buffer, CancellationToken cancelToken)
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

    private readonly struct MessageChunk
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
