using System.Collections.Generic;

namespace NLog.Contrib.LogListener;

public record LogListenerOptions
{
    public bool LogInternals { get; set; } = false;
    public IList<ListenerOptions> Listeners { get; set; } = new List<ListenerOptions>();
}
