using FluentAssertions;
using NLog.Contrib.LogListener.Tcp.Deserializers;
using NLog.Contrib.LogListener.Tcp.Listeners;
using NSubstitute;
using Xunit;

namespace NLog.Contrib.LogListener.Tcp.UnitTests.Listeners;

public class NLogClient_Tests
{
    private readonly INetworkChannel _channel = Substitute.For<INetworkChannel>();
    private readonly ILogger _clientLogger = Substitute.For<ILogger>();
    private readonly INLogDeserializer _deserializer = Substitute.For<INLogDeserializer>();

    private NLogClient CreateTestee()
        => new(this._channel, this._clientLogger, this._deserializer);

    [Theory]
    [InlineData("Bar")]
    [InlineData("")]
    public void ChannelDataReceived_DeserializerTryExtractSucceeded_ShouldSetCurrentDataToLeftover(string expected)
    {
        // Arrange
        var testee = this.CreateTestee();
        var eventArgs = new ReceivedEventArgs("Foo"u8.ToArray());
        this._deserializer.TryExtract(new ExtractInput("Foo")).Returns(new ExtractResult(true, null, expected));

        // Act
        testee.ChannelDataReceived(this, eventArgs);

        // Assert
        testee.CurrentDataString.Should().Be(expected);
    }

    [Fact]
    public void ChannelDataReceived__ShouldBeRegistered()
    {
        // Arrange

        // Act
        var testee = this.CreateTestee();

        // Assert
        this._channel.Received().DataReceived += testee.ChannelDataReceived;
    }

    [Fact]
    public void ChannelDataReceived_DeserializerTryExtractDidntSucceed_ShouldAppendCurrentData()
    {
        // Arrange
        var testee = this.CreateTestee();
        var eventArgs1 = new ReceivedEventArgs("Foo"u8.ToArray());
        var eventArgs2 = new ReceivedEventArgs("Bar"u8.ToArray());
        this._deserializer.TryExtract(Arg.Any<ExtractInput>()).Returns(ci => new ExtractResult(false, null, ci.Arg<ExtractInput>().Data));

        // Act
        testee.ChannelDataReceived(this, eventArgs1);
        testee.ChannelDataReceived(this, eventArgs2);

        // Assert
        testee.CurrentDataString.Should().Be("FooBar");
    }

    [Fact]
    public void ChannelDataReceived_DeserializerTryExtractSucceed_ShouldCallClientLogWithResult()
    {
        // Arrange
        var testee = this.CreateTestee();
        var eventArgs = new ReceivedEventArgs("Foo"u8.ToArray());
        var expectedLog = LogEventInfo.Create(LogLevel.Debug, "Bar", "Baz");
        this._deserializer.TryExtract(new ExtractInput("Foo")).Returns(new ExtractResult(true, expectedLog, ""));

        // Act
        testee.ChannelDataReceived(this, eventArgs);

        // Assert
        this._clientLogger.Received().Log(expectedLog);
    }

    [Fact]
    public void ChannelDataReceived_DeserializerTryExtractSucceedMultipleTimes_ShouldCallClientLogWithResult()
    {
        // Arrange
        var testee = this.CreateTestee();
        var eventArgs = new ReceivedEventArgs("Foo1Foo2"u8.ToArray());
        var expectedLog1 = LogEventInfo.Create(LogLevel.Debug, "Bar1", "Baz1");
        var expectedLog2 = LogEventInfo.Create(LogLevel.Debug, "Bar2", "Baz2");
        this._deserializer.TryExtract(new ExtractInput("Foo1Foo2")).Returns(new ExtractResult(true, expectedLog1, "Foo2"));
        this._deserializer.TryExtract(new ExtractInput("Foo2")).Returns(new ExtractResult(true, expectedLog2, ""));

        // Act
        testee.ChannelDataReceived(this, eventArgs);

        // Assert
        Received.InOrder(
            () =>
            {
                this._clientLogger.Log(expectedLog1);
                this._clientLogger.Log(expectedLog2);
            });
    }

    [Fact]
    public void ChannelDisconnected__ShouldBeRegistered()
    {
        // Arrange

        // Act
        var testee = this.CreateTestee();

        // Assert
        this._channel.Received().Disconnected += testee.ChannelDisconnected;
    }
}
