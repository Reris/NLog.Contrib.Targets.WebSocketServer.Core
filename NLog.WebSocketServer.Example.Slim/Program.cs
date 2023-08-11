using NLog.Contrib.LogListener;
using NLog.Contrib.Targets.WebSocketServer.Core;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseNLog();
builder.Services.AddNLogTargetWebSocket();
builder.Services.AddLogListener(o => o.Tcp.Add(new TcpListenerOptions { Formats = new[] { "json" } }));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseNLogWebSockets();
app.Services.StartLogListeners();

app.Run();
