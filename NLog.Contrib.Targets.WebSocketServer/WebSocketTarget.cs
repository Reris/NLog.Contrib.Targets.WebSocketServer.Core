using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog.Targets;

namespace NLog.Contrib.Targets.WebSocketServer;

[PublicAPI]
[Target("NLog.Contrib.Targets.WebSocketServer")]
public class WebSocketTarget : TargetWithLayout
{
    private LogEntryDistributor? _distributor;
    private bool _enabled;

    /// <summary>
    /// By default NLog.Contrib.Targets.WebSocketServerTarget will fail silently if does not succeed trying to set up the websocket server
    /// (e.g.: because the port is already in use), and it will be automatically disabled. In production you may not want the application to crash because
    /// one of your targets failed, but during development you would like to get an exception indicatig the issue.
    /// </summary>
    public bool ThrowExceptionIfSetupFails { get; set; }

    /// <summary>
    /// The maximum number of allowed connections. By default 100.
    /// </summary>
    public int MaxConnectedClients { get; set; } = 100;

    protected override void InitializeTarget()
    {
        try
        {
            this._distributor = new LogEntryDistributor(this.MaxConnectedClients);
            this._enabled = true;
        }
        catch
        {
            this._enabled = false;
            if (this.ThrowExceptionIfSetupFails)
            {
                throw;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!this._enabled)
        {
            return;
        }

        this._distributor?.Dispose();
        base.Dispose(disposing);
    }

    protected override void Write(LogEventInfo logEvent)
    {
        if (!this._enabled)
        {
            return;
        }

        try
        {
            this._distributor?.Broadcast(this.Layout.Render(logEvent));
        }
        catch
        {
            // munch
        }
    }

    public bool Subscribe(IWebSocket con) => this._distributor?.TryAddWebSocketToPool(con) == true;

    public void Unsubscribe(IWebSocket con) => this._distributor?.RemoveDisconnected(con);

    public ValueTask AcceptWebSocketCommands(string message, IWebSocket webSocket)
        => this._distributor?.AcceptWebSocketCommandsAsync(message, webSocket) ?? ValueTask.CompletedTask;
}
