﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Contrib.LogListener.Listeners;

[ExcludeFromCodeCoverage]
public class NetworkChannel : INetworkChannel
{
    private static readonly ILogger Logger = InternalLogger.Get<NetworkChannel>();
    private readonly CancellationTokenSource _cancellationTokenSource;

    public NetworkChannel(INetworkClient client)
    {
        this.Client = client ?? throw new ArgumentNullException(nameof(client));
        this.Stream = this.Client.GetStream();
        this._cancellationTokenSource = new CancellationTokenSource();
        this.Start(this._cancellationTokenSource.Token);
    }

    private NetworkStream Stream { get; }
    private INetworkClient Client { get; }

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        this._cancellationTokenSource.Dispose();
        this.Stream.Dispose();
        this.Client.Dispose();
        GC.SuppressFinalize(this);
    }

    public EndPoint RemoteEndPoint => this.Client.RemoteEndPoint ?? throw new NoEndpointException();

    public event EventHandler<ReceivedEventArgs>? DataReceived;
    public event EventHandler? Disconnected;

    private async void Start(CancellationToken cancellationToken)
    {
        await Task.Yield();

        const int bufferSize = 65000;
        try
        {
            var buffer = new byte[bufferSize];
            int slice;
            while (this.Client.Connected
                   && (slice = await this.Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                this.DataReceived?.Invoke(this, new ReceivedEventArgs(buffer.AsMemory(0, slice)));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        catch (Exception e)
        {
            NetworkChannel.Logger.Fatal(e, "The NetworkChannel of '{RemoteEndpoint}' died", this.RemoteEndPoint.ToString());
        }
        finally
        {
            this.Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
