using System;
using FluentAssertions;
using NLog.Contrib.LogListener.Data;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using Xunit;

namespace NLog.Contrib.LogListener.UnitTests.Deserializers.Formats;

public class JsonFormat_Tests
{
    private JsonFormat CreateTestee()
        => new();

    [Theory]
    [InlineData("{\"logge", true)]
    [InlineData(" \r\n \t {\"logge", true)]
    [InlineData("logge", false)]
    [InlineData("<log4", false)]
    [InlineData("", false)]
    public void HasValidStart_ShouldReturn(string data, bool expected)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);

        // Act
        var result = testee.HasValidStart(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(
        """{"logger":"Foo","level":"VERBOSE","message":"Bar"}""",
        """{"logger":"Foo","level":"VERBOSE","message":"Bar"}""")]
    [InlineData(
        """{"logger":"Foo","level":"InFO","message":"Bar"}{"log""",
        """{"logger":"Foo","level":"InFO","message":"Bar"}""")]
    [InlineData(
        """{"logger":"Foo","level":"InFO","message":"Ba""",
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
    [InlineData("""{"level":"TRACE"}""", NLogLevel.Trace)]
    [InlineData("""{"level":"debug"}""", NLogLevel.Debug)]
    [InlineData("""{"level":"Info"}""", NLogLevel.Info)]
    [InlineData("""{"level":"warn"}""", NLogLevel.Warn)]
    [InlineData("""{"level":"ErROR"}""", NLogLevel.Error)]
    [InlineData("""{"level":"FATAL"}""", NLogLevel.Fatal)]
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
    [InlineData("""{"level":"VERBOSE"}""", NLogLevel.Trace)]
    [InlineData("""{"level":"debug"}""", NLogLevel.Debug)]
    [InlineData("""{"level":"Information"}""", NLogLevel.Info)]
    [InlineData("""{"level":"warning"}""", NLogLevel.Warn)]
    [InlineData("""{"level":"ErROR"}""", NLogLevel.Error)]
    [InlineData("""{"level":"FATAL"}""", NLogLevel.Fatal)]
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

    [Theory]
    [InlineData("""{"timestamp":"2023-09-09T14:25:47.4423593+02:00","sourceContext":"Foo","level":"TRACE","message":"Bar","processName":"Company.MyApp","machineName":"97d38438edf9"}""")]
    public void Deserialize_NLogFullLogStash_ShouldReturnFilled(string data)
    {
        // Arrange
        var testee = this.CreateTestee();
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.TimeStamp.Should().Be(DateTime.Parse("2023-09-09T14:25:47.4423593+02:00"));
        result.LoggerName.Should().Be("Foo");
        result.Level.Should().Be(LogLevel.Trace);
        result.Message.Should().Be("Bar");
        result.Properties["@pn"].Should().Be("Company.MyApp");
        result.Properties["@mn"].Should().Be("97d38438edf9");
    }
    
    [Theory]
    [InlineData("""{"@t":"2023-09-09T14:25:47.4423593+02:00","SourceContext":"Foo","@l":"TRACE","@m":"Bar","ProcessName":"Company.MyApp","MachineName":"97d38438edf9"}""")]
    [InlineData("""{"@t":"2023-09-09T14:25:47.4423593+02:00","@s":"Foo","@l":"TRACE","@m":"Bar","@pn":"Company.MyApp","@mn":"97d38438edf9"}""")]
    public void Deserialize_NLogFullCompact_ShouldReturnFilled(string data)
    {
        // Arrange
        var testee = this.CreateTestee();
        testee.Configure(new JsonFormat.Options { Schemes = new [] { "compact" } });
        var input = this.CreateInput(data);
        var range = new Range(0, data.Length);

        // Act
        var result = testee.Deserialize(input, range);

        // Assert
        result.TimeStamp.Should().Be(DateTime.Parse("2023-09-09T14:25:47.4423593+02:00"));
        result.LoggerName.Should().Be("Foo");
        result.Level.Should().Be(LogLevel.Trace);
        result.Message.Should().Be("Bar");
        result.Properties["@pn"].Should().Be("Company.MyApp");
        result.Properties["@mn"].Should().Be("97d38438edf9");
    }

    private ExtractInput CreateInput(string data)
        => new(data, new ListenerOptions { Formats = { new JsonFormat.Options() } });
}
