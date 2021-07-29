using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Contrib.Targets.WebSocket;
using NLog.Targets.Wrappers;
using Owin.WebSocket;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    [WebSocketRoute(Constants.PATH_WSLOGGER_LISTENER)]
    public class WebSocketLogListener : WebSocketConnection
    {

        public WebSocketLogListener(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public override async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            var cMessage = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);

            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketTarget webSocketServerTarget)
                {
                    await webSocketServerTarget.AcceptWebSocketCommands(cMessage, mWebSocket);
                }
            }
        }

        public override void OnOpen()
        {
            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketTarget webSocketServerTarget)
                {
                    webSocketServerTarget.Subscribe(mWebSocket);
                }
                else if (target is WrapperTargetBase wrapperTarget && wrapperTarget.WrappedTarget is WebSocketTarget wrapperWebSocketServerTarget)
                {
                    wrapperWebSocketServerTarget.Subscribe(mWebSocket);
                }
            }
        }
        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            foreach (var target in LogManager.Configuration.ConfiguredNamedTargets)
            {
                if (target is WebSocketTarget webSocketServerTarget)
                {
                    webSocketServerTarget.Unsubscribe(mWebSocket);
                }
            }
        }
        public override bool AuthenticateRequest(HttpRequest request)
        {
            return true;
        }
    }
}