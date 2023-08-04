using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer;

public interface IWebSocketClient
{
    IWebSocket WebSocket { get; }
    Regex? Expression { get; set; }
}