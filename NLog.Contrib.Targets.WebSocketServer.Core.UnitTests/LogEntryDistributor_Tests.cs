using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.Targets.WebSocketServer.Core.UnitTests;

public class LogEntryDistributor_Tests
{
    private LogEntryDistributor CreateTestee() => new();

    [Fact]
    public async Task Broadcast__ShouldSend()
    {
        // Arrange
        const string expectedEntry = "Foo log";
        var testee = this.CreateTestee();
        var expected = JsonSerializer.SerializeToUtf8Bytes(new LogEntry(expectedEntry), testee.SerializerOptions);

        var tcs = new TaskCompletionSource();
        var socket = Substitute.For<IWebSocket>();
        socket.WhenForAnyArgs(a => a.SendTextAsync(default!, default)).Do(_ => tcs.SetResult());
        testee.TryAddWebSocketToPool(socket);

        // Act
        testee.Broadcast(expectedEntry);

        // Assert
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await socket.Received().SendTextAsync(Arg.Is<ArraySegment<byte>>(a => a.SequenceEqual(expected)), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TryAddWebSocketToPool__ShouldAddClient()
    {
        // Arrange
        var testee = this.CreateTestee();
        var socket = Substitute.For<IWebSocket>();

        // Act
        var result = testee.TryAddWebSocketToPool(socket);

        // Assert
        result.Should().BeTrue();
        testee.Clients.Should()
              .HaveCount(1)
              .And.Contain(a => a.WebSocket == socket);
    }
}
