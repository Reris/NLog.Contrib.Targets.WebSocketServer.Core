using System.Collections.Generic;

namespace NLog.Contrib.LogListener;

public record LogListenerOptions
{
    public bool LogInternals { get; set; } = false;
    public IList<TcpListenerOptions> Tcp { get; set; } = new List<TcpListenerOptions>();
}
