using System.Text.Json.Nodes;

namespace NLog.Contrib.Targets.WebSocketServer.Core.CommandHandlers;

public interface ICommandHandler
{
    bool CanHandle(string commandName);
    void Handle(JsonObject command, WebSocketClient client);
}
