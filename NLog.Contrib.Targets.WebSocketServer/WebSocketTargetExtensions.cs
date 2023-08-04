using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NLog.Contrib.Targets.WebSocketServer.WebSocketConnections;

namespace NLog.Contrib.Targets.WebSocketServer;

public static class WebSocketTargetExtensions
{
    public static IServiceCollection AddNLogTargetWebSocket(this IServiceCollection services, Action<LogViewerOptions>? configureOptions = null)
    {
        if (services.All(s => s.ServiceType != typeof(IHttpContextAccessor)))
        {
            services.AddHttpContextAccessor();
        }

        services.AddTransient<LogViewerWebSocketConnection>();
        services.AddSingleton<WebSocketConnectionMiddleware<LogViewerWebSocketConnection>>();

        if (configureOptions is not null)
        {
            services.AddOptions<LogViewerOptions>(typeof(WebSocketConnectionMiddleware<>).Name)
                    .Configure(configureOptions);
        }

        return services;
    }

    public static IApplicationBuilder UseNLogWebSockets(this IApplicationBuilder app)
        => app.UseNLogWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

    public static IApplicationBuilder UseNLogWebSockets(this IApplicationBuilder app, WebSocketOptions options)
    {
        app.UseWebSockets(options);

        app.UseMiddleware<WebSocketConnectionMiddleware<LogViewerWebSocketConnection>>();

        return app;
    }
}