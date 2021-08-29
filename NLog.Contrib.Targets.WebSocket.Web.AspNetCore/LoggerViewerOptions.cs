using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    public class LoggerViewerOptions
    {
        public string RootPath { get; set; }
        public WebSocketOptions WebSocketOptions { get; set; }
    }
}
