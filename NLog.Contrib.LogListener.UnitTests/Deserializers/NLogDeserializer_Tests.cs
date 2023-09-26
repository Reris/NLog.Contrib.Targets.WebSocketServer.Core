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
        """<log4j:event logger="Foo" level="VERBOSE"><log4j:message>Bar</log4j:message></log4j:event>""",
        NLogLevel.Trace,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """<log4j:event logger="Foo" level="InFO"><log4j:message>Bar</log4j:message></log4j:event>""",
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
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input, false);

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
        """{"sourceContext":"Foo","level":"VERBOSE","message":"Bar"}""",
        NLogLevel.Trace,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """{"sourceContext":"Foo","level":"InFO","message":"Bar"}""",
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """{"sourceContext":"Foo","level":"INFO","message":"Bar","app":"Company.MyApp","machinename":"97d38438edf9"}""",
        NLogLevel.Info,
        "Foo",
        "Bar",
        "")]
    [InlineData(
        """{"sourceContext":"Foo","level":"DEBUG","message":"Baz"}""" + """{"sourceCo""",
        NLogLevel.Debug,
        "Foo",
        "Baz",
        """{"sourceCo""")]
    [InlineData(
        """{"sourceContext":"Foo","level":"DEBUG","message":"Baz"}{"sourceContext":"Bar","level":"INFO","message":"Foobar"}""",
        NLogLevel.Debug,
        "Foo",
        "Baz",
        """{"sourceContext":"Bar","level":"INFO","message":"Foobar"}""")]
    public void TryExtract_CompleteJson_ShouldExtract(string data, NLogLevel level, string logger, string message, string leftover)
    {
        // Arrange
        var testee = this.CreateTestee();
        var expectedLevel = LogLevel.FromOrdinal((int)level);
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input, false);

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
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input);

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
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Result.Should().BeNull();
        result.LeftoverString.Should().Be(leftover);
    }

    [Theory]
    [InlineData(" foo")]
    [InlineData("<Foo />")]
    public void TryExtract_Unknown_ShouldScrap(string data)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Result.Should().BeNull();
        result.LeftoverString.Should().BeEmpty();
    }
    
    [Theory]
    [InlineData("<log4")]
    [InlineData("""{"logge""")]
    [InlineData(" \t\n <log4")]
    [InlineData(" \t\n {\"logge")]
    public void TryExtract_Incomplete_ShouldReturn(string data)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);

        // Act
        var result = testee.TryExtract(input);

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
        var input = this.CreateInput("");

        // Act
        var result = testee.TryExtract(input);

        // Assert
        result.Should().Be(new ExtractResult(false, null, ""));
    }

    private ExtractInput CreateInput(string data)
        => new(data, new ListenerOptions { Formats = { new JsonFormat.Options(), new Log4JXmlFormat.Options() } });
}
