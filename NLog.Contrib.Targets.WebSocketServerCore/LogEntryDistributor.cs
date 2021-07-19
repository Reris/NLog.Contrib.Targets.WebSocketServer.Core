using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Contrib.Targets.WebSocketServer.CommandHandlers;
using Owin.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NLog.Contrib.Targets.WebSocketServer
{

    public sealed class LogEntryDistributor : IDisposable
    {
        readonly BufferBlock<LogEntry> _block;
        readonly CancellationTokenSource _cancel;
        readonly ReaderWriterLockSlim _semaphore;
        readonly List<WebSocketWrapper> _connections;
        readonly JsonSerializer _serializer;
        readonly String _ipAddressStart;
        readonly Int32 _maxConnectedClients;

        readonly ICommandHandler[] _commandHandlers;

        Int32 _disposed;

        public LogEntryDistributor(Int32 maxConnectedClients, TimeSpan clientTimeout)
        {
            _maxConnectedClients = maxConnectedClients;
            _cancel = new CancellationTokenSource();
            _block = new BufferBlock<LogEntry>(new DataflowBlockOptions() { CancellationToken = _cancel.Token });
            _connections = new List<WebSocketWrapper>();
            _serializer = new JsonSerializer();
            _semaphore = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            _commandHandlers = new[] { new FilterCommandHandler() };

            Task.Run((Func<Task>)ReceiveAndBroadcast);
        }

        internal Boolean TryAddWebSocketToPool(IWebSocket con)
        {
            try
            {
                _semaphore.EnterWriteLock();

                if (_connections.Count >= _maxConnectedClients)
                    return false;

                var ws = new WebSocketWrapper(con);
                _connections.Add(ws);
                return true;
            }
            finally
            {
                _semaphore.ExitWriteLock();
            }
        }

        private async Task ReceiveAndBroadcast()
        {
            while (!_cancel.IsCancellationRequested)
            {
                var message = await _block.ReceiveAsync(_cancel.Token).ConfigureAwait(false);
                ParallelBroadcastLogEntry(message);
            }
        }

        private Int64 GetTimestamp(DateTime dateTime)
        {
            return Int64.Parse(dateTime.ToString("yyyyMMddHHmmssff"));
        }

        public void Broadcast(String logline, DateTime timestamp)
        {
            if (!_cancel.IsCancellationRequested)
            {
                _block.Post(new LogEntry(GetTimestamp(timestamp), logline));
            }
        }

        private void ParallelBroadcastLogEntry(LogEntry logEntry)
        {
            try
            {
                _semaphore.EnterReadLock();
                Parallel.ForEach(
                    source: _connections,
                    parallelOptions: new ParallelOptions()
                    {
                        CancellationToken = _cancel.Token,
                        MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                    },
                    body: ws => SendLogEntry(ws, logEntry));
            }
            finally
            {
                _semaphore.ExitReadLock();
            }
        }

        private void SendLogEntry(WebSocketWrapper ws, LogEntry logEntry)
        {
            try
            {
                //if (ws.WebSocket.State != WebSocketState.Open)
                //    return;

                if (ws.Expression != null && !ws.Expression.IsMatch(logEntry.Line))
                    return;

                ws.WebSocket.SendText(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(logEntry))), true, _cancel.Token);
            }
            catch (Exception ex)
            {
            }
        }

        internal async Task AcceptWebSocketCommands(string message, IWebSocket webSocket)
        {
            try
            {
                if (message == null) // server shutting down
                    return;

                var json = JObject.Parse(message);
                var command = json.Property("command");

                if (command == null || command.Value == null)
                    return;

                var socket = _connections.FirstOrDefault(w => w.WebSocket == webSocket);
                if (socket == null)
                {
                    return;
                }
                HandleCommand(command.Value.ToString(), json, socket);
            }
            catch
            {
            }
        }

        private void HandleCommand(String commandName, JObject json, WebSocketWrapper wsWrapper)
        {
            try
            {
                foreach (var handler in _commandHandlers.Where(h => h.CanHandle(commandName)))
                {
                    handler.Handle(json, wsWrapper);
                }
            }
            catch (Exception)
            {
            }
        }

        //private void RemoveDisconnected()
        //{
        //    try
        //    {
        //        _semaphore.EnterUpgradeableReadLock();

        //        var disconnected = _connections.Where(c => !c.WebSocket.IsConnected).ToList();
        //        if (!disconnected.Any())
        //            return;

        //        RemoveDisconnected(disconnected);
        //    }
        //    finally
        //    {
        //        _semaphore.ExitUpgradeableReadLock();
        //    }
        //}

        internal void RemoveDisconnected(IWebSocket webSocket)
        {
            try
            {
                _semaphore.EnterWriteLock();

                var socket = _connections.FirstOrDefault(w => w.WebSocket == webSocket);
                if (socket != null)
                {
                    _connections.Remove(socket);
                }
            }
            finally
            {
                _semaphore.ExitWriteLock();
            }
        }

        private void Dispose(Boolean disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            _cancel.Cancel();
            _semaphore.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~LogEntryDistributor()
        {
            Dispose(false);
        }
    }

}
