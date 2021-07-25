using System;
using System.Collections.Generic;
using System.Text;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    internal class Constants
    {
        internal const string PATH_WSLOGGER = "/wslogger";

        internal const string PATH_WSLOGGER_LISTENER = PATH_WSLOGGER + "/listen";

        internal const string PATH_VIEWER = PATH_WSLOGGER + "/viewer";
        internal const string PATH_VIEWER_INDEX = PATH_WSLOGGER + "/viewer/index";

        internal const string VIEWER_DIR = "WebSocketLoggerViewer";
        internal const string VIEWER_INDEX = "/index.html";

        internal const string EXTENSION_HTML = ".html";
        internal const string EXTENSION_JS = ".js";
        internal const string EXTENSION_CSS = ".css";

        internal const string MEDIATYPE_HTML = "text/html; charset=utf-8";
        internal const string MEDIATYPE_JS = "application/javascript; charset=utf-8";
        internal const string MEDIATYPE_CSS = "text/css; charset=utf-8";
    }
}
