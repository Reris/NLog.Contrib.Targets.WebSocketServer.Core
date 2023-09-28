using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace Logair;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static void Main(string[] args) => Program.CreateWebHostBuilder(args).Build().Run();

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        // We dont need to log our own ASP-calls
        var logairOptions = new NLogAspNetCoreOptions
        {
            CaptureMessageProperties = false,
            CaptureMessageTemplates = false,
            RegisterHttpContextAccessor = false
        };

        return WebHost.CreateDefaultBuilder(args)
                      .UseNLog()
                      .ConfigureServices(_ => LogManager.Configuration = Program.AddNullTargetForNonVerbose(LogManager.Configuration))
                      .ConfigureAppConfiguration((_, config) => config.AddJsonFile("appsettings-listeners.json", false, true))
                      .UseInternalLogger(logairOptions)
                      .UseStartup<Startup>();
    }

    private static LoggingConfiguration AddNullTargetForNonVerbose(LoggingConfiguration configuration)
    {
        var verboseString = Environment.GetEnvironmentVariable("LOGAIR_VERBOSELEVEL")?.ToLower().Trim() ?? nameof(LogLevel.Warn);
        LogLevel muteLevel;
        try
        {
            var verbose = LogLevel.FromString(verboseString);
            if (verbose == LogLevel.Trace)
            {
                return configuration;
            }

            muteLevel = LogLevel.FromOrdinal(verbose.Ordinal - 1);
        }
        catch (ArgumentException)
        {
            muteLevel = LogLevel.Info;
        }

        var ruleItem = new LoggingRule("logair.*", LogLevel.Trace, muteLevel, new NullTarget()) { Final = true };
        configuration.LoggingRules.Insert(0, ruleItem);
        return configuration;
    }
}
