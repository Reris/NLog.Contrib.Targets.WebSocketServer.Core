using System;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

public class FilterCommandHandler : ICommandHandler
{
    public bool CanHandle(string commandName) => string.Equals("filter", commandName, StringComparison.OrdinalIgnoreCase);

    public void Handle(JsonObject command, IWebSocketClient wswrapper)
    {
        if (command.TryGetPropertyValue("filter", out var node) && node?.GetValue<string>() is { } filter)
        {
            wswrapper.Expression = new Regex(filter);
        }
    }
}