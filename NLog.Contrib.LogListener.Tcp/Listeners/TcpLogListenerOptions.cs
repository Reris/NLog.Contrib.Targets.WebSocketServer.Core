using System.Net;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

public record TcpLogListenerOptions
{   
    public IPEndPoint EndPoint { get; init; } = new(IPAddress.Any, 4505);
}
