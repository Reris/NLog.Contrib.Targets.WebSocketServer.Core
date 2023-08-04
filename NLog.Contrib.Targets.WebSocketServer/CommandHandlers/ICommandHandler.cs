using System.Text.Json.Nodes;

namespace NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

public interface ICommandHandler
{
    bool CanHandle(string commandName);
    void Handle(JsonObject command, IWebSocketClient wswrapper);
}