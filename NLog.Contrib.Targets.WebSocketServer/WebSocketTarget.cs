using System;
using System.Threading.Tasks;
using NLog.Targets;

namespace NLog.Contrib.Targets.WebSocketServer;

[Target("NLog.Contrib.Targets.WebSocketServer")]
public class WebSocketTarget : TargetWithLayout
{
    private LogEntryDistributor? _distributor;
    private bool _enabled;

    public bool ThrowExceptionIfSetupFails { get; set; } = true;
    public int MaxConnectedClients { get; set; } = 100;
    public TimeSpan ClientTimeOut { get; set; } = TimeSpan.FromSeconds(5);

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
            this._distributor?.Broadcast(this.Layout.Render(logEvent), logEvent.TimeStamp);
        }
        catch
        {
            // munch
        }
    }

    public bool Subscribe(IWebSocket con) => this._distributor?.TryAddWebSocketToPool(con) == true;

    public void Unsubscribe(IWebSocket con) => this._distributor?.RemoveDisconnected(con);

    public Task AcceptWebSocketCommands(string message, IWebSocket webSocket)
        => this._distributor?.AcceptWebSocketCommands(message, webSocket) ?? Task.CompletedTask;
}