using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NLog.Contrib.LogListener.Listeners;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Listeners;

public class TcpLogListener_Tests
{
    private readonly ILogClientFactory _clientFactory = Substitute.For<ILogClientFactory>();
    private readonly IOptionsMonitor<LogListenerOptions> _optionsMonitor = TcpLogListener_Tests.CreateOptionsMonitor();
    private readonly INetworkProviderFactory _providerFactory = Substitute.For<INetworkProviderFactory>();

    private static IOptionsMonitor<LogListenerOptions> CreateOptionsMonitor()
    {
        var result = Substitute.For<IOptionsMonitor<LogListenerOptions>>();
        result.CurrentValue.Returns(new LogListenerOptions());
        return result;
    }

    private TcpLogListener CreateTestee()
        => new(this._providerFactory, this._clientFactory, this._optionsMonitor);

    [Fact]
    public void Options__ShouldBeOptionsListeners()
    {
        // Arrange
        var expected = new[] { new ListenerOptions() };
        this._optionsMonitor.CurrentValue.Listeners = expected;
        var testee = this.CreateTestee();

        // Act
        var result = testee.Options;

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void Start_ProviderConnected_ShouldBeCreateLogClient()
    {
        // Arrange
        var channel = Substitute.For<INetworkChannel>();
        var connected = new ConnectedEventArgs(channel);
        var provider = this._providerFactory.Create<ITcpNetworkListener>();
        var options = new ListenerOptions();
        this._optionsMonitor.CurrentValue.Listeners = new[] { options };
        var testee = this.CreateTestee();

        // Act
        testee.Start();
        provider.Connected += Raise.EventWith(connected);

        // Assert
        this._clientFactory.Received().CreateFor(channel, options);
    }

    [Theory]
    [InlineData("v4", 1231, "0.0.0.0:1231")]
    [InlineData("v6", 1232, "[::]:1232")]
    [InlineData("0.0.0.1", 1233, "0.0.0.1:1233")]
    [InlineData("[::1]", 1234, "[::1]:1234")]
    public void Start__ShouldCallProviderConnect(string ip, int port, string expected)
    {
        // Arrange
        var provider = this._providerFactory.Create<ITcpNetworkListener>();
        var options = new ListenerOptions { Ip = ip, Port = port };
        this._optionsMonitor.CurrentValue.Listeners = new[] { options };
        var testee = this.CreateTestee();
        var expectedEndPoint = IPEndPoint.Parse(expected);

        // Act
        testee.Start();

        // Assert
        provider.Received().Connect(expectedEndPoint);
    }
}
