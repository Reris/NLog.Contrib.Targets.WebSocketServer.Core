using System;
using System.Net;
using System.Net.Sockets;

namespace NLog.Contrib.LogListener.Listeners;

/// <summary>
/// Adapter/Wrapper for <see cref="TcpClient" />,<see cref="UdpClient" />, etc.
/// </summary>
public interface INetworkClient : IDisposable
{
    bool Connected { get; }
    EndPoint RemoteEndPoint { get; }
    NetworkStream GetStream();
}
