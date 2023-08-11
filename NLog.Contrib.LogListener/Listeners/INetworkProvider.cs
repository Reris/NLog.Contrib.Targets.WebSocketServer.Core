using System;
using System.Net;

namespace NLog.Contrib.LogListener.Listeners;

public interface INetworkProvider : IDisposable
{
    void Connect(EndPoint endPoint);
    event EventHandler<ConnectedEventArgs> Connected;
}
