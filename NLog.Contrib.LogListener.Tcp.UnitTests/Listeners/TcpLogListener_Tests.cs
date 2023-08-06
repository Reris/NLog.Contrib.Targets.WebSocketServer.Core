using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NLog.Contrib.LogListener.Tcp.Listeners;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.LogListener.Tcp.UnitTests.Listeners;

public class TcpLogListener_Tests
{
    private readonly ILogClientFactory _clientFactory = Substitute.For<ILogClientFactory>();
    private readonly ITcpNetworkListener _provider = Substitute.For<ITcpNetworkListener>();
    private readonly IOptionsMonitor<TcpLogListenerOptions> _optionsMonitor = TcpLogListener_Tests.CreateOptionsMonitor();

    private static IOptionsMonitor<TcpLogListenerOptions> CreateOptionsMonitor()
    {
        var result = Substitute.For<IOptionsMonitor<TcpLogListenerOptions>>();
        result.CurrentValue.Returns(new TcpLogListenerOptions());
        return result;
    }

    private TcpLogListener CreateTestee()
        => new(this._provider, this._clientFactory, this._optionsMonitor);

    [Fact]
    public void ListeningEndpoint__ShouldBeOptionsEndPoint()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = new TcpLogListenerOptions().EndPoint;

        // Act
        var result = testee.ListeningEndPoint;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ProviderOnConnected__ShouldBeCreateLogClient()
    {
        // Arrange
        this.CreateTestee();
        var channel = Substitute.For<INetworkChannel>();
        var connected = new ConnectedEventArgs(channel);

        // Act
        this._provider.Connected += Raise.EventWith(connected);

        // Assert
        this._clientFactory.Received().CreateFor(channel);
    }

    [Fact]
    public void Start__ShouldCallsProviderConnectWithListeningEndPoint()
    {
        // Arrange
        var testee = this.CreateTestee();

        // Act
        testee.Start();

        // Assert
        this._provider.Received().Connect(testee.ListeningEndPoint);
    }
}
