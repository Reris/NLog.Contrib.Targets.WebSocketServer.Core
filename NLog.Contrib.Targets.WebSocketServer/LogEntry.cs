namespace NLog.Contrib.Targets.WebSocketServer;

public class LogEntry
{
    public LogEntry(long timestamp, string logLine)
    {
        this.Timestamp = timestamp;
        this.Line = logLine;
    }

    public long Timestamp { get; private set; }
    public string Line { get; private set; }
}