using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NLog.Contrib.LogListener.Listeners;

[ExcludeFromCodeCoverage]
public class TcpNetworkProvider : INetworkProvider
{
    private static readonly ILogger Logger = InternalLogger.Get<TcpNetworkProvider>();

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
                this.Connected?.Invoke(this, new ConnectedEventArgs(new NetworkChannel(new NetworkClient(client))));
            }
        }
        catch (Exception e)
        {
            TcpNetworkProvider.Logger.Fatal(e, $"The {nameof(TcpNetworkProvider)} died.");
        }
    }

    private readonly struct NetworkClient : INetworkClient
    {
        private readonly TcpClient _client;

        public NetworkClient(TcpClient client)
        {
            this._client = client;
        }

        public void Dispose() => this._client.Dispose();
        public bool Connected => this._client.Connected;
        public EndPoint RemoteEndPoint => this._client.Client.RemoteEndPoint;
        public NetworkStream GetStream() => this._client.GetStream();
    }
}
