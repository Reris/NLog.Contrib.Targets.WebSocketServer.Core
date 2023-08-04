using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog.Contrib.Targets.WebSocketServer.CommandHandlers;

namespace NLog.Contrib.Targets.WebSocketServer;

public class LogEntryDistributor : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly BufferBlock<LogEntry> _block;

    private readonly ICommandHandler[] _commandHandlers;
    private readonly List<IWebSocketClient> _connections;
    private readonly CancellationTokenSource _cts;
    private readonly int _maxConnectedClients;
    private readonly ReaderWriterLockSlim _semaphore;

    private int _disposed;

    public LogEntryDistributor(int maxConnectedClients)
    {
        this._maxConnectedClients = maxConnectedClients;
        this._cts = new CancellationTokenSource();
        this._block = new BufferBlock<LogEntry>(new DataflowBlockOptions { CancellationToken = this._cts.Token });
        this._connections = new List<IWebSocketClient>();
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

            var ws = new WebSocketClient(con);
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

    public void Broadcast(string logline, DateTime timestamp)
    {
        if (!this._cts.IsCancellationRequested)
        {
            this._block.Post(new LogEntry(logline));
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

    private void SendLogEntry(IWebSocketClient ws, LogEntry logEntry)
    {
        try
        {
            if (ws.Expression is not null && !ws.Expression.IsMatch(logEntry.Entry))
            {
                return;
            }

            
            ws.WebSocket.SendText(
                new ArraySegment<byte>(JsonSerializer.SerializeToUtf8Bytes(logEntry, LogEntryDistributor.SerializerOptions)),
                true,
                this._cts.Token);
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

            var json = JsonNode.Parse(message)?.AsObject();
            var command = json?["command"];

            if (json is null || command is null)
            {
                return Task.CompletedTask;
            }

            var socket = this._connections.FirstOrDefault(w => w.WebSocket == webSocket);
            if (socket is null)
            {
                return Task.CompletedTask;
            }

            this.HandleCommand(command.GetValue<string>(), json, socket);
        }
        catch
        {
            // munch
        }

        return Task.CompletedTask;
    }

    private void HandleCommand(string commandName, JsonObject json, IWebSocketClient wsClient)
    {
        try
        {
            foreach (var handler in this._commandHandlers.Where(h => h.CanHandle(commandName)))
            {
                handler.Handle(json, wsClient);
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
