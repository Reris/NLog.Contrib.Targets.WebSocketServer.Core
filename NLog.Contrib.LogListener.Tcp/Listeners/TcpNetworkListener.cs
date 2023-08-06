using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

[ExcludeFromCodeCoverage]
public class TcpNetworkListener : ITcpNetworkListener
{
    private static readonly ILogger Logger = InternalLogger.Get<TcpNetworkListener>();

    private TcpListener? _listener;

    public void Dispose()
    {
        this._listener?.Stop();
        this._listener = null;
        GC.SuppressFinalize(this);
    }

    public void Connect(EndPoint endPoint)
    {
        if (this._listener != null)
        {
            throw new InvalidOperationException("Listener already started.");
        }

        if (endPoint is not IPEndPoint ipEndPoint)
        {
            throw new ArgumentException($"Must be an {nameof(IPEndPoint)}.", nameof(endPoint));
        }

        this._listener = new TcpListener(ipEndPoint);
        this.StartAsync();
    }

    public event EventHandler<ConnectedEventArgs>? Connected;

    private async void StartAsync()
    {
        this._listener?.Start();
        await Task.Yield();

        try
        {
            while (this._listener is { } listener)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                this.Connected?.Invoke(this, new ConnectedEventArgs(new TcpNetworkChannel(client)));
            }
        }
        catch (Exception e)
        {
            TcpNetworkListener.Logger.Fatal(e, $"The {nameof(TcpNetworkListener)} died.");
        }
    }
}
