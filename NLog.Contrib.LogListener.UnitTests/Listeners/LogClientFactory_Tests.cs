using FluentAssertions;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Listeners;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Listeners;

public class LogClientFactory_Tests
{
    private readonly ILogger _clientLogger = Substitute.For<ILogger>();
    private readonly IDeserializerFactory _deserializerFactory = Substitute.For<IDeserializerFactory>();

    private LogClientFactory CreateTestee()
        => new(this._clientLogger, this._deserializerFactory);

    [Fact]
    public void CreateFor__ShouldCreateLogClient()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expectedChannel = Substitute.For<INetworkChannel>();
        var options = new ListenerOptions();
        var expectedDeserializer = this._deserializerFactory.Get<INLogDeserializer>(options);

        // Act
        var result = testee.CreateFor(expectedChannel, options);

        // Assert
        result.Should().BeOfType<NLogClient>();
        var client = (NLogClient)result;
        client.Channel.Should().BeSameAs(expectedChannel);
        client.ClientLogger.Should().BeSameAs(this._clientLogger);
        client.Deserializer.Should().BeSameAs(expectedDeserializer);
    }
}
