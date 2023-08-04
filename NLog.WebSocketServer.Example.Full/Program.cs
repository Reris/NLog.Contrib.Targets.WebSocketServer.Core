using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Contrib.Targets.WebSocketServer;
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
builder.Services.AddNLogTargetWebSocket();
builder.Services.AddControllersWithViews();

var app = builder.Build();
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
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    "default",
    "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

LogTicker();

app.Run();

static async void LogTicker()
{
    var i = 0;
    var logger = LogManager.GetCurrentClassLogger();
    while (true)
    {
        logger.Info("Log tick {Tick}", ++i);
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}