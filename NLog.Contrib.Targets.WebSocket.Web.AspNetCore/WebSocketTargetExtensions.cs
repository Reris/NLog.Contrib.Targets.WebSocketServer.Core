using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    public static class WebSocketTargetExtensions
    {
        public static IServiceCollection AddNLogTargetWebSocket(this IServiceCollection services)
        {
            if (!services.Any(s => s.ServiceType == typeof(IHttpContextAccessor)))
            {
                services.AddHttpContextAccessor();
            }

            services.AddTransient<WebSocketLogListener>();
            services.AddSingleton<WebSocketConnectionMiddleware<WebSocketLogListener>>();

            return services;
        }
        public static IApplicationBuilder UseNLogWebSockets(this IApplicationBuilder app, WebSocketOptions options)
        {
            app.UseWebSockets(options);

            app.UseMiddleware<WebSocketConnectionMiddleware<WebSocketLogListener>>();

            return app;
        }
    }
}
