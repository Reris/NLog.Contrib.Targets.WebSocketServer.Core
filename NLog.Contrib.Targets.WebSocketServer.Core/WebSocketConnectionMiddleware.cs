using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog.Contrib.Targets.WebSocketServer.Core.WebSocketConnections;

namespace NLog.Contrib.Targets.WebSocketServer.Core;

public class WebSocketConnectionMiddleware<T> : IMiddleware
    where T : WebSocketConnection
{
    private readonly LogViewerOptions _logViewerOptions;
    private readonly IServiceProvider _serviceProvider;

    public WebSocketConnectionMiddleware(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;

        this._logViewerOptions = serviceProvider.GetRequiredService<IOptionsMonitor<LogViewerOptions>>()
                                                .Get(typeof(WebSocketConnectionMiddleware<>).Name);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requestPath = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        requestPath = !string.IsNullOrWhiteSpace(this._logViewerOptions.RootPath)
                          ? requestPath.Replace(this._logViewerOptions.RootPath, string.Empty, StringComparison.OrdinalIgnoreCase)
                          : requestPath;
        if (!requestPath.StartsWith(this._logViewerOptions.PathWslogger, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
        }
        else if (requestPath.StartsWith(this._logViewerOptions.PathViewer, StringComparison.OrdinalIgnoreCase))
        {
            // serve viewer
            try
            {
                if (requestPath.EndsWith(this._logViewerOptions.PathViewer))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                    context.Response.Headers.TryAdd("Location", requestPath + this._logViewerOptions.ViewerIndex);
                    await context.Response.WriteAsync(string.Empty, Encoding.UTF8);

                    return;
                }

                var file = await EmbeddedFileHelper.GetFileAsync("ViewerSpa", requestPath, this._logViewerOptions.ViewerIndex);
                context.Response.ContentType = file.ContentType;
                await context.Response.WriteAsync(file.Content, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                await next(context);
            }
        }
        else if (requestPath.StartsWith(this._logViewerOptions.PathWsloggerListener, StringComparison.OrdinalIgnoreCase))
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socketConnection = this._serviceProvider.GetRequiredService<T>();
                await socketConnection.AcceptSocketAsync(context);
            }
        }
    }
}