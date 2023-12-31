﻿using System.Text.RegularExpressions;

namespace NLog.Contrib.Targets.WebSocketServer.Core;

public class WebSocketClient
{
    public WebSocketClient(IWebSocket webSocket)
    {
        this.WebSocket = webSocket;
    }

    public IWebSocket WebSocket { get; }
    public Regex? Expression { get; set; }
}