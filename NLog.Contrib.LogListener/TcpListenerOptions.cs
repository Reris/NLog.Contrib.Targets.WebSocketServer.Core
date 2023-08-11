using JetBrains.Annotations;

namespace NLog.Contrib.LogListener;

[PublicAPI]
public record TcpListenerOptions : DeserializerOptions
{
    public string Ip { get; set; } = "v4";
    public int Port { get; set; } = 4505;
}
