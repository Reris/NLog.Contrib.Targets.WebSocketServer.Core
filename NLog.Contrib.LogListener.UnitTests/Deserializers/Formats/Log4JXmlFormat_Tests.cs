using System;
using FluentAssertions;
using NLog.Contrib.LogListener.Data;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Deserializers.Formats;

public class Log4JXmlFormat_Tests
{
    private Log4JXmlFormat CreateTestee()
        => new();

    [Theory]
    [InlineData(
        """<log4j:event logger="Foo" level="TRACE"><log4j:message>Bar</log4j:message></log4j:event>""",
        """<log4j:event logger="Foo" level="TRACE"><log4j:message>Bar</log4j:message></log4j:event>""")]
    [InlineData(
        """<log4j:event logger="Foo" level="VERBOSE"><log4j:message>Bar</log4j:message></log4j:event><log4j:ev""",
        """<log4j:event logger="Foo" level="VERBOSE"><log4j:message>Bar</log4j:message></log4j:event>""")]
    [InlineData(
        """<log4j:event logger="Foo" level="WARN"><log4j:messa""",
        "")]
    [InlineData("", "")]
    public void GetSlice_ShouldReturnSlice(string data, string expected)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);

        // Act
        var result = testee.GetSlice(input);

        // Assert
        var output = data[result];
        output.Should().Be(expected);
    }

    [Theory]
    [InlineData("""<log4j:event logger="Foo" level="TRACE"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Trace)]
    [InlineData("""<log4j:event logger="Foo" level="debug"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Debug)]
    [InlineData("""<log4j:event logger="Foo" level="Info"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Info)]
    [InlineData("""<log4j:event logger="Foo" level="warn"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Warn)]
    [InlineData("""<log4j:event logger="Foo" level="ErROR"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Error)]
    [InlineData("""<log4j:event logger="Foo" level="FATAL"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Fatal)]
    public void Deserialize_NLogLevel_ShouldReturnNLogLevel(string data, NLogLevel expected)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.Level.Should().Be(LogLevel.FromOrdinal((int)expected));
    }

    [Theory]
    [InlineData("""<log4j:event logger="Foo" level="VERBOSE"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Trace)]
    [InlineData("""<log4j:event logger="Foo" level="debug"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Debug)]
    [InlineData("""<log4j:event logger="Foo" level="Information"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Info)]
    [InlineData("""<log4j:event logger="Foo" level="warning"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Warn)]
    [InlineData("""<log4j:event logger="Foo" level="ErROR"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Error)]
    [InlineData("""<log4j:event logger="Foo" level="FATAL"><log4j:message>Bar</log4j:message></log4j:event>""", NLogLevel.Fatal)]
    public void Deserialize_SerilogLevel_ShouldReturnNLogLevel(string data, NLogLevel expected)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.Level.Should().Be(LogLevel.FromOrdinal((int)expected));
    }

    [Fact]
    public void Deserialize_NLogFullStandard_ShouldReturnFilled()
    {
        // Arrange
        const string data =
            """
            <log4j:event logger="Foo" level="TRACE" timestamp="1694262721767"><log4j:message>Bar</log4j:message><log4j:properties>
            <log4j:data name="log4japp" value="Company.MyApp" /><log4j:data name="log4jmachinename" value="97d38438edf9" /></log4j:properties>
            </log4j:event>
            """;
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.LoggerName.Should().Be("Foo");
        result.Level.Should().Be(LogLevel.Trace);
        result.TimeStamp.Should().Be(DateTime.UnixEpoch.AddMilliseconds(1694262721767));
        result.Message.Should().Be("Bar");
        result.Properties["@pn"].Should().Be("Company.MyApp");
        result.Properties["@mn"].Should().Be("97d38438edf9");
    }

    private ExtractInput CreateInput(string data) => new(data, new ListenerOptions { Formats = { new Log4JXmlFormat.Options() } });
}
