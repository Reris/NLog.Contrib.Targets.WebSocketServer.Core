using Microsoft.Owin;
using Owin;
using System;
using System.Threading.Tasks;
using Owin.WebSocket.Extensions;

[assembly: OwinStartup(typeof(WebFormApplication.Startup))]

namespace WebFormApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 有关如何配置应用程序的详细信息，请访问 https://go.microsoft.com/fwlink/?LinkID=316888

            //For static routes http://foo.com/ws use MapWebSocketRoute and attribute the WebSocketConnection with [WebSocketRoute('/ws')]
            app.MapWebSocketRoute<LogListener>("/ws");
        }
    }
}
