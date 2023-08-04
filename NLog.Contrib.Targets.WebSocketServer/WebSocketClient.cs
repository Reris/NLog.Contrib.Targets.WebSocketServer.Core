using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer;

public class WebSocketClient : IWebSocketClient
{
    public WebSocketClient(IWebSocket webSocket)
    {
        this.WebSocket = webSocket;
    }

    public IWebSocket WebSocket { get; }
    public Regex? Expression { get; set; }
}