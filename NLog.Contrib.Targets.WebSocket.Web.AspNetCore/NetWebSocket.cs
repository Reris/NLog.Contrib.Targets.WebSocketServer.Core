using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NLog.Contrib.Targets.WebSocket.Web.AspNetCore.Handlers;
using Owin.WebSocket;
using Owin.WebSocket.Extensions;

namespace NLog.Contrib.Targets.WebSocket.Web.AspNetCore
{
    class NetWebSocket : IWebSocket
    {
        private readonly TaskQueue mSendQueue;
        private readonly System.Net.WebSockets.WebSocket mWebSocket;

        public NetWebSocket(System.Net.WebSockets.WebSocket webSocket)
        {
            mWebSocket = webSocket;
            mSendQueue = new TaskQueue();
        }

        public TaskQueue SendQueue
        {
            get { return mSendQueue; }
        }

        public WebSocketCloseStatus? CloseStatus
        {
            get { return mWebSocket.CloseStatus; }
        }

        public string CloseStatusDescription
        {
            get { return mWebSocket.CloseStatusDescription; }
        }

        public Task SendText(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        {
            return Send(data, WebSocketMessageType.Text, endOfMessage, cancelToken);
        }

        public Task SendBinary(ArraySegment<byte> data, bool endOfMessage, CancellationToken cancelToken)
        {
            return Send(data, WebSocketMessageType.Binary, endOfMessage, cancelToken);
        }

        public Task Send(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancelToken)
        {
            var sendContext = new SendContext(data, endOfMessage, messageType, cancelToken);

            return mSendQueue.Enqueue(
                async s =>
                {
                    await mWebSocket.SendAsync(s.Buffer, s.Type, s.EndOfMessage, s.CancelToken);
                },
                sendContext);
        }

        public Task Close(WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken cancelToken)
        {
            return mWebSocket.CloseAsync(closeStatus, closeDescription, cancelToken);
        }

        public async Task<Tuple<ArraySegment<byte>, WebSocketMessageType>> ReceiveMessage(byte[] buffer, CancellationToken cancelToken)
        {
            var count = 0;
            WebSocketReceiveResult result;
            do
            {
                var segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                result = await mWebSocket.ReceiveAsync(segment, cancelToken);

                count += result.Count;
            }
            while (!result.EndOfMessage);

            return new Tuple<ArraySegment<byte>, WebSocketMessageType>(new ArraySegment<byte>(buffer, 0, count), result.MessageType);
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    mWebSocket?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~NetWebSocket()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
