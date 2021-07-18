using Owin.WebSocket;
using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer
{
    public interface IWebSocketWrapper
    {
        IWebSocket WebSocket { get; }
        Regex Expression { get; set; }
    }
    public sealed class WebSocketWrapper : IWebSocketWrapper
    {
        public IWebSocket WebSocket { get; private set; }
        public Regex Expression { get; set; }

        public WebSocketWrapper(IWebSocket webSocket)
        {
            WebSocket = webSocket;
        }
    }
}
