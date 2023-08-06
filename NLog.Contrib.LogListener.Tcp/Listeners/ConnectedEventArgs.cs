using System;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

public class ConnectedEventArgs : EventArgs
{
    public ConnectedEventArgs(INetworkChannel channel)
    {
        this.Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public INetworkChannel Channel { get; }
}
