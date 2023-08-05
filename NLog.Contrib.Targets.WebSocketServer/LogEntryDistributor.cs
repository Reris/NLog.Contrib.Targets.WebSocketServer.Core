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
    private readonly BufferBlock<LogEntry> _block;
    private readonly List<WebSocketClient> _clients = new();

    private readonly ICommandHandler[] _commandHandlers = { new FilterCommandHandler() };
    private readonly CancellationTokenSource _cts = new();
    private readonly int _maxConnectedClients;
    private readonly ReaderWriterLockSlim _semaphore = new(LockRecursionPolicy.NoRecursion);
    public readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private int _disposed;

    public LogEntryDistributor(int maxConnectedClients = 100)
    {
        this._maxConnectedClients = maxConnectedClients;
        this._block = new BufferBlock<LogEntry>(new DataflowBlockOptions { CancellationToken = this.RunningCancellationToken });

        Task.Run(this.ReceiveAndBroadcastAsync);
    }

    public CancellationToken RunningCancellationToken => this._cts.Token;
    public IReadOnlyList<WebSocketClient> Clients => this._clients;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose(true);
    }

    public bool TryAddWebSocketToPool(IWebSocket con)
    {
        this._semaphore.EnterWriteLock();
        try
        {
            if (this.Clients.Count >= this._maxConnectedClients)
            {
                return false;
            }

            var ws = new WebSocketClient(con);
            this._clients.Add(ws);
            return true;
        }
        finally
        {
            this._semaphore.ExitWriteLock();
        }
    }

    private async Task ReceiveAndBroadcastAsync()
    {
        while (!this._cts.IsCancellationRequested)
        {
            var message = await this._block.ReceiveAsync(this.RunningCancellationToken).ConfigureAwait(false);
            await this.ParallelBroadcastLogEntryAsync(message);
        }
    }

    public void Broadcast(string logline)
    {
        if (!this._cts.IsCancellationRequested)
        {
            this._block.Post(new LogEntry(logline));
        }
    }

    private async Task ParallelBroadcastLogEntryAsync(LogEntry logEntry)
    {
        WebSocketClient[] clients;
        this._semaphore.EnterReadLock();
        try
        {
            clients = this._clients.ToArray();
        }
        finally
        {
            this._semaphore.ExitReadLock();
        }

        await Parallel.ForEachAsync(
            clients,
            new ParallelOptions
            {
                CancellationToken = this.RunningCancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            },
            (client, ct) => this.SendLogEntryAsync(client, logEntry, ct));
    }

    private async ValueTask SendLogEntryAsync(WebSocketClient client, LogEntry logEntry, CancellationToken cancellationToken)
    {
        try
        {
            if (client.Expression is not null && !client.Expression.IsMatch(logEntry.Entry))
            {
                return;
            }


            await client.WebSocket.SendTextAsync(
                new ArraySegment<byte>(JsonSerializer.SerializeToUtf8Bytes(logEntry, this.SerializerOptions)),
                true,
                cancellationToken);
        }
        catch
        {
            // munch
        }
    }

    public ValueTask AcceptWebSocketCommandsAsync(string? message, IWebSocket webSocket)
    {
        try
        {
            if (message is null) // server shutting down
            {
                return ValueTask.CompletedTask;
            }

            var json = JsonNode.Parse(message)?.AsObject();
            var command = json?["command"];

            if (json is null || command is null)
            {
                return ValueTask.CompletedTask;
            }

            var socket = this.Clients.FirstOrDefault(w => w.WebSocket == webSocket);
            if (socket is null)
            {
                return ValueTask.CompletedTask;
            }

            this.HandleCommand(command.GetValue<string>(), json, socket);
        }
        catch
        {
            // munch
        }

        return ValueTask.CompletedTask;
    }

    private void HandleCommand(string commandName, JsonObject json, WebSocketClient wsClient)
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

    public void RemoveDisconnected(IWebSocket webSocket)
    {
        this._semaphore.EnterWriteLock();
        try
        {
            var socket = this.Clients.FirstOrDefault(w => w.WebSocket == webSocket);
            if (socket != null)
            {
                this._clients.Remove(socket);
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

        this._cts.Cancel();
        this._semaphore.Dispose();
    }

    ~LogEntryDistributor()
    {
        this.Dispose(false);
    }
}
