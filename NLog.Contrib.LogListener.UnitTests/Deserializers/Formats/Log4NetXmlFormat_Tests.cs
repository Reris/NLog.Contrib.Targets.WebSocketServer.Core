using System;
using FluentAssertions;
using NLog.Contrib.LogListener.Data;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Deserializers.Formats;

public class Log4NetXmlFormat_Tests
{
    private Log4NetXmlFormat CreateTestee()
        => new();

    [Theory]
    [InlineData(
        """<log4net:event logger="Foo" level="TRACE"><log4net:message>Bar</log4net:message></log4net:event>""",
        """<log4net:event logger="Foo" level="TRACE"><log4net:message>Bar</log4net:message></log4net:event>""")]
    [InlineData(
        """<log4net:event logger="Foo" level="VERBOSE"><log4net:message>Bar</log4net:message></log4net:event><log4net:ev""",
        """<log4net:event logger="Foo" level="VERBOSE"><log4net:message>Bar</log4net:message></log4net:event>""")]
    [InlineData(
        """<log4net:event logger="Foo" level="WARN"><log4net:messa""",
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
    [InlineData("""<log4net:event logger="Foo" level="TRACE"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Trace)]
    [InlineData("""<log4net:event logger="Foo" level="debug"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Debug)]
    [InlineData("""<log4net:event logger="Foo" level="Info"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Info)]
    [InlineData("""<log4net:event logger="Foo" level="warn"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Warn)]
    [InlineData("""<log4net:event logger="Foo" level="ErROR"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Error)]
    [InlineData("""<log4net:event logger="Foo" level="FATAL"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Fatal)]
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
    [InlineData("""<log4net:event logger="Foo" level="VERBOSE"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Trace)]
    [InlineData("""<log4net:event logger="Foo" level="debug"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Debug)]
    [InlineData("""<log4net:event logger="Foo" level="Information"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Info)]
    [InlineData("""<log4net:event logger="Foo" level="warning"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Warn)]
    [InlineData("""<log4net:event logger="Foo" level="ErROR"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Error)]
    [InlineData("""<log4net:event logger="Foo" level="FATAL"><log4net:message>Bar</log4net:message></log4net:event>""", NLogLevel.Fatal)]
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
            <log4net:event logger="Foo" timestamp="2023-09-09T14:25:47.4423593+02:00" level="TRACE" xmlns:log4net="https://logging.apache.org/log4net/schemas/log4net-events-1.2/">
            <log4net:message>Bar</log4net:message><log4net:properties>
            <log4net:data name="ProcessName" value="Company.MyApp" /><log4net:data name="log4net:HostName" value="97d38438edf9" /></log4net:properties>
            </log4net:event>
            """;
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.LoggerName.Should().Be("Foo");
        result.Level.Should().Be(LogLevel.Trace);
        result.TimeStamp.Should().Be(DateTime.Parse("2023-09-09T14:25:47.4423593+02:00"));
        result.Message.Should().Be("Bar");
        result.Properties["@pn"].Should().Be("Company.MyApp");
        result.Properties["@mn"].Should().Be("97d38438edf9");
    }

    private ExtractInput CreateInput(string data) => new(data, new ListenerOptions { Formats = { new Log4NetXmlFormat.Options() } });
}
