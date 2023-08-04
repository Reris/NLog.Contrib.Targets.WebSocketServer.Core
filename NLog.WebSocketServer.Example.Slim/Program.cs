using NLog.Contrib.Targets.WebSocketServer;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseNLog();
builder.Services.AddNLogTargetWebSocket();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseNLogWebSockets();

app.Run();
