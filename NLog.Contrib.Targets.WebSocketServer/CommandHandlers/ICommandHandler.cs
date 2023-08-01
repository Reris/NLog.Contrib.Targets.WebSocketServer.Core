using Newtonsoft.Json.Linq;

namespace NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

public interface ICommandHandler
{
    bool CanHandle(string commandName);
    void Handle(JObject command, IWebSocketWrapper wswrapper);
}