using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NLog.Contrib.LogListener;
using NLog.Contrib.Targets.WebSocketServer.Core;
using NLog.Web;

// ReSharper disable once ConvertToConstant.Local
#if DEBUG
var devMode = true;
#else
var devMode = false;
#endif

if (devMode && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    Environment.SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "Microsoft.AspNetCore.SpaProxy");
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLog();
builder.Services.AddNLogTargetWebSocket()
       .AddLogListener(builder.Configuration.GetSection("LogListener"))
       .AddControllersWithViews();

var app = builder.Build();

// Use wwwdist to be a second wwwroot. It's easier to delete and add the whole folder when building the SPA
if (app.Environment.WebRootFileProvider is CompositeFileProvider compositeFileProvider)
{
    var wwwdist = Path.Combine(builder.Environment.ContentRootPath, @"wwwdist");
    Directory.CreateDirectory(wwwdist);
    app.Environment.WebRootFileProvider = new CompositeFileProvider(compositeFileProvider.FileProviders.Append(new PhysicalFileProvider(wwwdist)));
}

if (!app.Environment.IsDevelopment())
{
}

app.UseNLogWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(60) });
app.Services.StartLogListeners();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    "default",
    "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
