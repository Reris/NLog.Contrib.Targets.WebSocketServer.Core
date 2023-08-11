using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Contrib.LogListener.Listeners;

public interface INetworkChannel : IDisposable
{
    EndPoint RemoteEndPoint { get; }
    Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    event EventHandler? Disconnected;
    event EventHandler<ReceivedEventArgs>? DataReceived;
}
