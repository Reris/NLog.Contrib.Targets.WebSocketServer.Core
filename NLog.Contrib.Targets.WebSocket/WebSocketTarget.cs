using NLog.Config;
using NLog.Targets;
using Owin.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLog.Contrib.Targets.WebSocket
{
    [Target("NLog.Contrib.Targets.WebSocket")]
    public sealed class WebSocketTarget : TargetWithLayout
    {
        LogEntryDistributor _distributor;
        Boolean _enabled;

        [RequiredParameter]
        public Int32 Port { get; set; }

        public String IPAddressStartsWith { get; set; }

        public Boolean ThrowExceptionIfSetupFails { get; set; }

        public Int32 MaxConnectedClients { get; set; }

        public TimeSpan ClientTimeOut { get; set; }

        public WebSocketTarget()
        {
            MaxConnectedClients = 100;
            ClientTimeOut = TimeSpan.FromSeconds(5);
        }

        protected override void InitializeTarget()
        {
            try
            {
                _distributor = new LogEntryDistributor(MaxConnectedClients, ClientTimeOut);
                _enabled = true;
            }
            catch (Exception)
            {
                _enabled = false;
                if (ThrowExceptionIfSetupFails)
                    throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_enabled)
                return;

            _distributor.Dispose();
            base.Dispose(disposing);
        }
        protected override void Write(LogEventInfo logEvent)
        {
            if (!_enabled)
                return;
            try
            {
                _distributor.Broadcast(this.Layout.Render(logEvent), logEvent.TimeStamp);
            }
            catch
            {
                // Swallow exceptions during writting
            }
        }

        public bool Subscribe(IWebSocket con) => _distributor.TryAddWebSocketToPool(con);

        public void Unsubscribe(IWebSocket con) => _distributor.RemoveDisconnected(con);

        public async Task AcceptWebSocketCommands(string message, IWebSocket webSocket) => await _distributor.AcceptWebSocketCommands(message, webSocket);
    }
}
