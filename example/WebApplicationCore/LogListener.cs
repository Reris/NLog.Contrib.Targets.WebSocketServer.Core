using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Contrib.Targets.WebSocketServer;
using Owin.WebSocket;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebFormApplication
{
    [WebSocketRoute("/ws")]
    public class LogListener : WebSocketConnection
    {
        public override async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            switch (type)
            {
                case WebSocketMessageType.Text:
                    break;
                case WebSocketMessageType.Binary:
                    break;
                case WebSocketMessageType.Close:
                    break;
                default:
                    break;
            }

            var cMessage = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);

            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketServerTarget webSocketServerTarget)
                {
                    await webSocketServerTarget.AcceptWebSocketCommands(cMessage, mWebSocket);
                }
            }
        }

        public override void OnOpen()
        {
            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketServerTarget webSocketServerTarget)
                {
                    webSocketServerTarget.Subscribe(mWebSocket);
                }
            }
        }
        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketServerTarget webSocketServerTarget)
                {
                    webSocketServerTarget.Subscribe(mWebSocket);
                }
            }
        }
        public override bool AuthenticateRequest(HttpRequest request)
        {
            return true;
        }
    }
}