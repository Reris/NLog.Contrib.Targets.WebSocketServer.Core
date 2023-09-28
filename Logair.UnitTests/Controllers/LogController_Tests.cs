using FluentAssertions;
using JetBrains.Annotations;
using Logair;
using Logair.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using NSubstitute;
using Xunit;

namespace Cider.Mill.Logair.UnitTests.Controllers;

public class LogController_Tests
{
    private readonly ILogger _clientLogger;
    private readonly INLogDeserializer _deserializer;
    private readonly IOptionsMonitor<HttpListenerOptions> _options;

    [UsedImplicitly]
    public LogController_Tests()
    {
        this._deserializer = Substitute.For<INLogDeserializer>();
        this._clientLogger = Substitute.For<ILogger>();
        this._options = Substitute.For<IOptionsMonitor<HttpListenerOptions>>();
        this._options.CurrentValue.Returns(new HttpListenerOptions { Formats = { new JsonFormat.Options() } });
    }

    private LogController CreateTestee()
        => new(this._clientLogger, this._deserializer, this._options);

    [Fact]
    public void Post__ShouldLogExtractedMessageAndReturnOk()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string message = "Foo";
        var expectedMessage = new ExtractResult(true, new LogEventInfo(), string.Empty);
        this._deserializer.TryExtract(new ExtractInput(message, this._options.CurrentValue))
            .Returns(expectedMessage);

        // Act
        var result = testee.Post(message);

        // Assert
        this._clientLogger.Received().Log(expectedMessage.Result);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public void Post_InvalidRequest_ShouldReturnsBadRequest()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string message = "Foo";
        this._deserializer.TryExtract(new ExtractInput(message, this._options.CurrentValue))
            .Returns(new ExtractResult(false, null, string.Empty));

        // Act
        var result = testee.Post(message);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }
}
