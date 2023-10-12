// See https://aka.ms/new-console-template for more information

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NLog;
using Sender;
using Sender.Detail;
using Sender.Detail.Data;
using Serilog;
using Serilog.Events;

Parser.Default.ParseArguments<Options>(args)
      .WithParsed(
          o =>
          {
              var logger = o.ConfigFile.StartsWith("NLog.", StringComparison.OrdinalIgnoreCase)
                               ? GetNLogger(o)
                               : GetSeriLogger(o);

              var cts = new CancellationTokenSource();
              var currentRow = 0u;
              Loop(
                  async ct =>
                  {
                      var i = unchecked(++currentRow);
                      if (o.AddSize > 0 && i % o.AddSize == 0)
                      {
                          ++o.Size;
                      }

                      var message = "{row}: " + o.Message + new string('.', o.Size);
                      logger(i, message, i);
                      await Task.Delay(o.Interval, ct);
                  },
                  cts);

              Console.WriteLine("Press any key to stop");
              Loop(
                  async ct =>
                  {
                      if (!Console.KeyAvailable)
                      {
                          await Task.Delay(50, ct);
                          return;
                      }

                      Console.ReadKey();
                      Console.WriteLine("Finished!");
                      cts.Cancel();
                  },
                  cts).GetAwaiter().GetResult();
          });
return;

static Task Loop(Func<CancellationToken, Task> func, CancellationTokenSource cancellationTokenSource) => Task.Run(
    async () =>
    {
        var ct = cancellationTokenSource.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await func(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // munch
        }
        catch (Exception e)
        {
            Console.WriteLine("Stopped with exception:");
            var previousForgroundColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
            }
            finally
            {
                Console.ForegroundColor = previousForgroundColor;
            }
        }

        cancellationTokenSource.Cancel();
    });

static LoggerFunc GetNLogger(Options options)
{
    LogManager.Setup().LoadConfigurationFromFile(options.ConfigFile, false);
    var loggers = new[] { LogManager.GetLogger(typeof(Foo).FullName), LogManager.GetLogger(typeof(Bar).FullName), LogManager.GetLogger(typeof(Baz).FullName) };
    var rotate = 0u;
    return (level, messageTemplate, param) =>
    {
        var logger = loggers[unchecked(rotate++ % loggers.Length)];
        var logLevel = LogLevel.FromOrdinal(((int)level - 1) % 6);
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        logger.Log(logLevel, messageTemplate, param);
    };
}

static LoggerFunc GetSeriLogger(Options options)
{
    // NOTE: Serilog.Network tries to establish IPv6 connections if URI is named, like 'localhost' 
    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(options.ConfigFile)
                        .Build();
    var logConfig = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration);
    Log.Logger = logConfig.CreateLogger();
    var loggers = new[] { Log.Logger.ForContext<Foo>(), Log.Logger.ForContext<Bar>(), Log.Logger.ForContext<Baz>() };
    var rotate = 0u;
    return (level, messageTemplate, param) =>
    {
        var logger = loggers[unchecked(rotate++ % loggers.Length)];
        var logLevel = (LogEventLevel)((level - 1) % 6);
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        logger.Write(logLevel, messageTemplate, param);
    };
}

internal delegate void LoggerFunc(uint level, string messageTemplate, uint param);

namespace Sender
{
    public class Foo
    {
    }
}

namespace Sender.Detail
{
    public class Bar
    {
    }
}

namespace Sender.Detail.Data
{
    public class Baz
    {
    }
}

[PublicAPI]
#pragma warning disable CA1050
public record Options
{
    [Option('i', "interval", Required = false, HelpText = "Interval of a messages in ms.")]
    public int Interval { get; set; } = 1000;

    [Option('c', "config", Required = false, HelpText = "The config file of the testing logger.")]
    public string ConfigFile { get; set; } = "NLog.console.config";

    [Option('s', "size", Required = false, HelpText = "Starting size of a message.")]
    public int Size { get; set; } = 30;

    [Option('a', "add", Required = false, HelpText = "Add 1 size to each N message.")]
    public int AddSize { get; set; }

    [Option('m', "message", Required = false, HelpText = "The message. Or at least a part of it.")]
    public string? Message { get; set; }
}
