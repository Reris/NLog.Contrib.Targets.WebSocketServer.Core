using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Contrib.LogListener.Deserializers;

namespace Logair.Controllers;

[Route("[controller]")]
public class LogController : Controller
{
    private readonly IOptionsMonitor<HttpListenerOptions> _options;

    public LogController(ILogger clientLogger, INLogDeserializer deserializer, IOptionsMonitor<HttpListenerOptions> options)
    {
        this.ClientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
        this.Deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        this._options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ILogger ClientLogger { get; }
    public INLogDeserializer Deserializer { get; }

    [HttpPost]
    public IActionResult Post([FromBody] string message)
    {
        var result = this.Deserializer.TryExtract(new ExtractInput(message, this._options.CurrentValue));
        if (!result.Succeeded)
        {
            return this.BadRequest();
        }

        this.ClientLogger.Log(result.Result);
        return this.Ok();
    }
}
