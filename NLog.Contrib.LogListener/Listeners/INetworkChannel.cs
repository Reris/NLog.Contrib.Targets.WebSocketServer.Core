using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Contrib.LogListener.Listeners;

public interface INetworkChannel : IDisposable
{
    EndPoint RemoteEndPoint { get; }

    event EventHandler? Disconnected;
    event EventHandler<ReceivedEventArgs>? DataReceived;
}
