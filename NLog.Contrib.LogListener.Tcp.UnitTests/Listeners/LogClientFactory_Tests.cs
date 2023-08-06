using FluentAssertions;
using NLog.Contrib.LogListener.Tcp.Deserializers;
using NLog.Contrib.LogListener.Tcp.Listeners;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.LogListener.Tcp.UnitTests.Listeners;

public class LogClientFactory_Tests
{
    private readonly ILogger _clientLogger = Substitute.For<ILogger>();
    private readonly INLogDeserializer _deserializer = Substitute.For<INLogDeserializer>();

    private LogClientFactory CreateTestee()
        => new(this._clientLogger, this._deserializer);

    [Fact]
    public void CreateFor__ShouldCreateLogClient()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expectedChannel = Substitute.For<INetworkChannel>();

        // Act
        var result = testee.CreateFor(expectedChannel);

        // Assert
        result.Should().BeOfType<NLogClient>();
        var client = (NLogClient)result;
        client.Channel.Should().BeSameAs(expectedChannel);
        client.ClientLogger.Should().BeSameAs(this._clientLogger);
        client.Deserializer.Should().BeSameAs(this._deserializer);
    }
}
