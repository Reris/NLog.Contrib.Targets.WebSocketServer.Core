using System.Diagnostics;
using FluentAssertions;
using JetBrains.Annotations;
using NLog.Contrib.LogListener.Data;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Deserializers;

public class NLogDeserializer_Tests
{
    private readonly IFormat[] _deserializers;

    [UsedImplicitly]
    public NLogDeserializer_Tests()
    {
        this._deserializers = new IFormat[] { new JsonFormat(), new Log4JXmlFormat() };
    }

    private NLogDeserializer CreateTestee()
        => new(this._deserializers);

    [Theory]
    [InlineData(
        """<log4j:event logger="Foo" level="INFO"><log4j:message>Bar</log4j:message></log4j:event>""",
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """
        <log4j:event logger="Foo" level="INFO"><log4j:message>Bar</log4j:message><log4j:properties>
        <log4j:data name="log4japp" value="Company.MyApp" /><log4j:data name="log4jmachinename" value="97d38438edf9" /></log4j:properties>
        </log4j:event>
        """,
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """<log4j:event logger="Foo" level="DEBUG"><log4j:message>Baz</log4j:message></log4j:event>""" + "<log4j:event ",
        NLogLevel.Debug,
        "Foo",
        "Baz",
        "<log4j:event ")]
    [InlineData(
        """
        <log4j:event logger="Foo" level="DEBUG"><log4j:message>Baz</log4j:message></log4j:event>
        <log4j:event logger="Bar" level="INFO"><log4j:message>Foobar</log4j:message></log4j:event>
        """,
        NLogLevel.Debug,
        "Foo",
        "Baz",
        """

        <log4j:event logger="Bar" level="INFO"><log4j:message>Foobar</log4j:message></log4j:event>
        """)]
    public void TryExtract_CompleteLog4J_ShouldExtract(string data, NLogLevel level, string logger, string message, string leftover)
    {
        // Arrange
        var testee = this.CreateTestee();
        var expectedLevel = LogLevel.FromOrdinal((int)level);
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput(data, options), false);

        // Assert
        result.Succeeded.Should().BeTrue();
        Debug.Assert(result.Result is not null);
        result.Result.Level.Should().BeSameAs(expectedLevel);
        result.Result.LoggerName.Should().Be(logger);
        result.Result.Message.Should().Be(message);
        result.LeftoverString.Should().Be(leftover);
    }

    [Theory]
    [InlineData(
        """{"logger":"Foo","level":"INFO","message":"Bar"}""",
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """{"logger":"Foo","level":"INFO","message":"Bar","app":"Company.MyApp","machinename":"97d38438edf9"}""",
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """{"logger":"Foo","level":"DEBUG","message":"Baz"}""" + """{"logg""",
        NLogLevel.Debug,
        "Foo",
        "Baz",
        """{"logg""")]
    [InlineData(
        """{"logger":"Foo","level":"DEBUG","message":"Baz"}{"logger":"Bar","level":"INFO","message":"Foobar"}""",
        NLogLevel.Debug,
        "Foo",
        "Baz",
        """{"logger":"Bar","level":"INFO","message":"Foobar"}""")]
    public void TryExtract_CompleteJson_ShouldExtract(string data, NLogLevel level, string logger, string message, string leftover)
    {
        // Arrange
        var testee = this.CreateTestee();
        var expectedLevel = LogLevel.FromOrdinal((int)level);
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput(data, options), false);

        // Assert
        result.Succeeded.Should().BeTrue();
        Debug.Assert(result.Result is not null);
        result.Result.Level.Should().BeSameAs(expectedLevel);
        result.Result.LoggerName.Should().Be(logger);
        result.Result.Message.Should().Be(message);
        result.LeftoverString.Should().Be(leftover);
    }

    [Theory]
    [InlineData("<log4j:event logger=\"INVALIDXML></log4j:event><log4j:event", "<log4j:event")]
    public void TryExtract_InvalidLog4J_ShouldCutInvalidData(string data, string leftover)
    {
        // Arrange
        var testee = this.CreateTestee();
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput(data, options));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Result.Should().BeNull();
        result.LeftoverString.Should().Be(leftover);
    }

    [Theory]
    [InlineData("""{"logger":"Foo","level":"INF""", """{"logger":"Foo","level":"INF""")]
    public void TryExtract_InvalidJson_ShouldCutInvalidData(string data, string leftover)
    {
        // Arrange
        var testee = this.CreateTestee();
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput(data, options));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Result.Should().BeNull();
        result.LeftoverString.Should().Be(leftover);
    }

    [Theory]
    [InlineData("<Foo />")]
    public void TryExtract_UnknownOrIncomplete_ShouldReturn(string data)
    {
        // Arrange
        var testee = this.CreateTestee();
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput(data, options));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Result.Should().BeNull();
        result.LeftoverString.Should().Be(data);
    }

    [Fact]
    public void TryExtract_InputEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var testee = this.CreateTestee();
        var options = new DeserializerOptions();

        // Act
        var result = testee.TryExtract(new ExtractInput("", options));

        // Assert
        result.Should().Be(new ExtractResult(false, null, ""));
    }
}
