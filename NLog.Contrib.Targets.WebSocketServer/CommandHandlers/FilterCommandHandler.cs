using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

public class FilterCommandHandler : ICommandHandler
{
    public bool CanHandle(string commandName) => string.Equals("filter", commandName, StringComparison.OrdinalIgnoreCase);

    public void Handle(JObject command, IWebSocketClient wswrapper)
    {
        var expression = command.Property("filter");
        if (expression is null || expression.Value is null)
        {
            return;
        }

        wswrapper.Expression = new Regex(expression.Value.ToString());
    }
}