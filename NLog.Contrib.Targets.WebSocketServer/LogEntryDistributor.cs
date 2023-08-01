using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

namespace NLog.Contrib.Targets.WebSocketServer;

public class LogEntryDistributor : IDisposable
{
    private readonly BufferBlock<LogEntry> _block;
    private readonly CancellationTokenSource _cts;

    private readonly ICommandHandler[] _commandHandlers;
    private readonly List<IWebSocketWrapper> _connections;
    private readonly int _maxConnectedClients;
    private readonly ReaderWriterLockSlim _semaphore;

    private int _disposed;

    public LogEntryDistributor(int maxConnectedClients)
    {
        this._maxConnectedClients = maxConnectedClients;
        this._cts = new CancellationTokenSource();
        this._block = new BufferBlock<LogEntry>(new DataflowBlockOptions { CancellationToken = this._cts.Token });
        this._connections = new List<IWebSocketWrapper>();
        this._semaphore = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        this._commandHandlers = new ICommandHandler[] { new FilterCommandHandler() };

        Task.Run(this.ReceiveAndBroadcast);
    }

    public void Dispose() => this.Dispose(true);

    internal bool TryAddWebSocketToPool(IWebSocket con)
    {
        try
        {
            this._semaphore.EnterWriteLock();

            if (this._connections.Count >= this._maxConnectedClients)
            {
                return false;
            }

            var ws = new WebSocketWrapper(con);
            this._connections.Add(ws);
            return true;
        }
        finally
        {
            this._semaphore.ExitWriteLock();
        }
    }

    private async Task ReceiveAndBroadcast()
    {
        while (!this._cts.IsCancellationRequested)
        {
            var message = await this._block.ReceiveAsync(this._cts.Token).ConfigureAwait(false);
            this.ParallelBroadcastLogEntry(message);
        }
    }

    private long GetTimestamp(DateTime dateTime) => long.Parse(dateTime.ToString("yyyyMMddHHmmssff"));

    public void Broadcast(string logline, DateTime timestamp)
    {
        if (!this._cts.IsCancellationRequested)
        {
            this._block.Post(new LogEntry(this.GetTimestamp(timestamp), logline));
        }
    }

    private void ParallelBroadcastLogEntry(LogEntry logEntry)
    {
        try
        {
            this._semaphore.EnterReadLock();
            Parallel.ForEach(
                this._connections,
                new ParallelOptions
                {
                    CancellationToken = this._cts.Token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                },
                ws => this.SendLogEntry(ws, logEntry));
        }
        finally
        {
            this._semaphore.ExitReadLock();
        }
    }

    private void SendLogEntry(IWebSocketWrapper ws, LogEntry logEntry)
    {
        try
        {
            if (ws.Expression is not null && !ws.Expression.IsMatch(logEntry.Line))
            {
                return;
            }

            ws.WebSocket.SendText(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(logEntry))), true, this._cts.Token);
        }
        catch
        {
            // munch
        }
    }

    internal Task AcceptWebSocketCommands(string? message, IWebSocket webSocket)
    {
        try
        {
            if (message is null) // server shutting down
            {
                return Task.CompletedTask;
            }

            var json = JObject.Parse(message);
            var command = json.Property("command");

            if (command?.Value is null)
            {
                return Task.CompletedTask;
            }

            var socket = this._connections.FirstOrDefault(w => w.WebSocket == webSocket);
            if (socket is null)
            {
                return Task.CompletedTask;
            }

            this.HandleCommand(command.Value.ToString(), json, socket);
        }
        catch
        {
            // munch
        }

        return Task.CompletedTask;
    }

    private void HandleCommand(string commandName, JObject json, IWebSocketWrapper wsWrapper)
    {
        try
        {
            foreach (var handler in this._commandHandlers.Where(h => h.CanHandle(commandName)))
            {
                handler.Handle(json, wsWrapper);
            }
        }
        catch
        {
            // munch
        }
    }

    internal void RemoveDisconnected(IWebSocket webSocket)
    {
        try
        {
            this._semaphore.EnterWriteLock();

            var socket = this._connections.FirstOrDefault(w => w.WebSocket == webSocket);
            if (socket != null)
            {
                this._connections.Remove(socket);
            }
        }
        finally
        {
            this._semaphore.ExitWriteLock();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 1)
        {
            return;
        }

        if (disposing)
        {
            GC.SuppressFinalize(this);
        }

        this._cts.Cancel();
        this._semaphore.Dispose();
    }

    ~LogEntryDistributor()
    {
        this.Dispose(false);
    }
}