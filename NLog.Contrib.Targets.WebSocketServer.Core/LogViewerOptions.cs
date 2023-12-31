﻿using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;

namespace NLog.Contrib.Targets.WebSocketServer.Core;

[PublicAPI]
public class LogViewerOptions
{
    public LogViewerOptions()
    {
        this.RedirectEmptyToViewer = false;
        this.PathWslogger = "/wslogger";
        this.PathViewer = this.PathWslogger + "/viewer";
        this.PathWsloggerListener = this.PathWslogger + "/listen";
        this.ViewerIndex = "/index.html";
    }


    public string? RootPath { get; set; }
    public string PathWslogger { get; set; }

    public WebSocketOptions? WebSocketOptions { get; set; }
    public bool RedirectEmptyToViewer { get; set; }
    public string PathViewer { get; set; }
    public string PathWsloggerListener { get; set; }
    public string ViewerIndex { get; set; }
}
