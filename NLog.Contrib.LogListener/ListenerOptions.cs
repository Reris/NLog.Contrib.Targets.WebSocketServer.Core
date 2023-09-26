using System.Collections.Generic;
using System.Net.Sockets;
using JetBrains.Annotations;
using NLog.Contrib.LogListener.Deserializers.Formats;

namespace NLog.Contrib.LogListener;

[PublicAPI]
public record ListenerOptions
{
    /// <summary>
    /// Currently only Tcp is supported. Udp would need a consistent failure handling a user must be aware of.
    /// </summary>
    public ProtocolType Protocol => ProtocolType.Tcp;
    public string Ip { get; set; } = "v4";
    public int Port { get; set; } = 4505;
    public List<FormatOptions> Formats { get; set; } = new();
}
