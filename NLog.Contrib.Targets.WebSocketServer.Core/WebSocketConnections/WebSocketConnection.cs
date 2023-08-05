using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using NLog.Contrib.Targets.WebSocketServer.Core.Adapters;

namespace NLog.Contrib.Targets.WebSocketServer.Core.WebSocketConnections;

[PublicAPI]
public abstract class WebSocketConnection
{
    private readonly CancellationTokenSource _cts = new();
    private HttpContext? _context;
    private IWebSocket? _webSocket;

    protected IWebSocket WebSocket => this._webSocket ?? throw new NotRunningException();

    /// <summary>
    /// Owin context for the web socket
    /// </summary>
    public HttpContext Context => this._context ?? throw new NotRunningException();

    /// <summary>
    /// Maximum message size in bytes for the receive buffer
    /// </summary>
    public int MaxMessageSize { get; init; } = 1024 * 64;

    /// <summary>
    /// Closes the websocket connection
    /// </summary>
    public Task CloseAsync(WebSocketCloseStatus status, string reason) => this.WebSocket.CloseAsync(status, reason, CancellationToken.None);

    /// <summary>
    /// Aborts the websocket connection
    /// </summary>
    public void Abort() => this._cts.Cancel();

    /// <summary>
    /// Sends data to the client with binary message type
    /// </summary>
    /// <param name="buffer">Data to send</param>
    /// <param name="endOfMessage">End of the message?</param>
    /// <returns>Task to send the data</returns>
    public Task SendBinaryAsync(byte[] buffer, bool endOfMessage) => this.SendBinaryAsync(new ArraySegment<byte>(buffer), endOfMessage);

    /// <summary>
    /// Sends data to the client with binary message type
    /// </summary>
    /// <param name="buffer">Data to send</param>
    /// <param name="endOfMessage">End of the message?</param>
    /// <returns>Task to send the data</returns>
    public Task SendBinaryAsync(ArraySegment<byte> buffer, bool endOfMessage) => this.WebSocket.SendBinaryAsync(buffer, endOfMessage, this._cts.Token);

    /// <summary>
    /// Sends data to the client with the text message type
    /// </summary>
    /// <param name="buffer">Data to send</param>
    /// <param name="endOfMessage">End of the message?</param>
    /// <returns>Task to send the data</returns>
    public Task SendTextAsync(byte[] buffer, bool endOfMessage) => this.SendTextAsync(new ArraySegment<byte>(buffer), endOfMessage);

    /// <summary>
    /// Sends data to the client with the text message type
    /// </summary>
    /// <param name="buffer">Data to send</param>
    /// <param name="endOfMessage">End of the message?</param>
    /// <returns>Task to send the data</returns>
    public Task SendTextAsync(ArraySegment<byte> buffer, bool endOfMessage) => this.WebSocket.SendTextAsync(buffer, endOfMessage, this._cts.Token);

    /// <summary>
    /// Sends data to the client
    /// </summary>
    /// <param name="buffer">Data to send</param>
    /// <param name="endOfMessage">End of the message?</param>
    /// <param name="type">Message type of the data</param>
    /// <returns>Task to send the data</returns>
    public Task SendAsync(ArraySegment<byte> buffer, bool endOfMessage, WebSocketMessageType type)
        => this.WebSocket.SendAsync(buffer, type, endOfMessage, this._cts.Token);

    /// <summary>
    /// Verify the request
    /// </summary>
    /// <param name="request">Websocket request</param>
    /// <returns>True if the request is authenticated else false to throw unauthenticated and deny the connection</returns>
    public virtual bool AuthenticateRequest(HttpRequest request) => true;

    /// <summary>
    /// Verify the request asynchronously. Fires after AuthenticateRequest
    /// </summary>
    /// <param name="request">Websocket request</param>
    /// <returns>True if the request is authenticated else false to throw unauthenticated and deny the connection</returns>
    public virtual Task<bool> AuthenticateRequestAsync(HttpRequest request) => Task.FromResult(true);

    /// <summary>
    /// Fires after the websocket has been opened with the client
    /// </summary>
    public virtual ValueTask OnOpenAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Fires when data is received from the client
    /// </summary>
    /// <param name="message">Data that was received</param>
    /// <param name="type">Message type of the data</param>
    public virtual ValueTask OnMessageReceivedAsync(ArraySegment<byte> message, WebSocketMessageType type) => ValueTask.CompletedTask;

    /// <summary>
    /// Fires with the connection with the client has closed and after OnClose
    /// </summary>
    public virtual ValueTask OnCloseAsync(WebSocketCloseStatus? closeStatus, string? closeStatusDescription) => ValueTask.CompletedTask;

    /// <summary>
    /// Fires when an exception occurs in the message reading loop
    /// </summary>
    /// <param name="error">Error that occured</param>
    public virtual void OnReceiveError(Exception error)
    {
    }

    /// <summary>
    /// Receive one entire message from the web socket
    /// </summary>
    internal async Task AcceptSocketAsync(HttpContext context)
    {
        this._context = context;
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        if (await this.AuthenticateRequestAsync(context.Request))
        {
            var authorized = await this.AuthenticateRequestAsync(context.Request);
            if (authorized)
            {
                // user was authorized so accept the socket
                // accept(null, RunWebSocket);
                await this.RunWebSocketAsync(webSocket);
                return;
            }
        }

        // see if user was forbidden or unauthorized from previous authenticate request failure
        context.Response.StatusCode = context.User.Identity?.IsAuthenticated is true ? 403 : 401;
        this._context = null;
    }

    private async Task RunWebSocketAsync(WebSocket webSocket)
    {
        this._webSocket = new WebSocketAdapter(webSocket);

        await this.OnOpenAsync();

        var buffer = new byte[this.MaxMessageSize];
        IWebSocket.ReceivedMessage received;

        do
        {
            try
            {
                received = await this.WebSocket.ReceiveMessageAsync(buffer, this._cts.Token);
                if (received.Message.Count > 0)
                {
                    await this.OnMessageReceivedAsync(received.Message, received.Type);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (OperationCanceledException oce)
            {
                if (!this._cts.IsCancellationRequested)
                {
                    this.OnReceiveError(oce);
                }

                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (WebSocketConnection.IsFatalSocketException(ex))
                {
                    this.OnReceiveError(ex);
                }

                break;
            }
        } while (received.Type != WebSocketMessageType.Close);

        try
        {
            await this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, this._cts.Token);
        }
        catch
        {
            //Ignore
        }

        if (!this._cts.IsCancellationRequested)
        {
            this._cts.Cancel();
        }

        await this.OnCloseAsync(this.WebSocket.CloseStatus, this.WebSocket.CloseStatusDescription);
        this._webSocket = null;
    }

    internal static bool IsFatalSocketException(Exception ex)
    {
        // If this exception is due to the underlying TCP connection going away, treat as a normal close
        // rather than a fatal exception.
        if (ex is not COMException ce)
        {
            return true;
        }

        switch ((uint)ce.ErrorCode)
        {
            case 0x800703e3:
            case 0x800704cd:
            case 0x80070026:
                return false;
        }

        // unknown exception; treat as fatal
        return true;
    }

    public class NotRunningException : Exception
    {
    }
}
