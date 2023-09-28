using System;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using ILogger = NLog.ILogger;

namespace Logair;

public static class InternalLogger
{
    public static ILogger Get<TFor>() => LogManager.GetLogger("logair." + typeof(TFor).Name);

    public class Provider : ILoggerProvider
    {
        public Provider(NLogAspNetCoreOptions options)
        {
            this.InnerProvider = new NLogLoggerProvider(options);
        }

        public NLogLoggerProvider InnerProvider { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => this.InnerProvider.CreateLogger("logair." + categoryName);
    }
}
