using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer;

public interface IWebSocketWrapper
{
    IWebSocket WebSocket { get; }
    Regex? Expression { get; set; }
}