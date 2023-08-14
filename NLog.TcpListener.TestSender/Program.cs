// See https://aka.ms/new-console-template for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NLog;

Parser.Default.ParseArguments<Options>(args)
      .WithParsed(
          o =>
          {
              LogManager.Setup().LoadConfigurationFromFile(o.NLogConfigFile, false);
              var cts = new CancellationTokenSource();
              var logger = LogManager.GetCurrentClassLogger();
              var currentRow = 0u;
              Loop(
                  async ct =>
                  {
                      var i = unchecked(++currentRow);
                      if (o.AddSize > 0 && i % o.AddSize == 0)
                      {
                          ++o.Size;
                      }

                      var message = "{row}: " +o.Message+ new string('.', o.Size);
                      // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                      logger.Log(LogLevel.FromOrdinal(((int)i - 1) % 7), message, i);
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

static Task Loop(Func<CancellationToken, Task> func, CancellationTokenSource cancellationTokenSource)
    => Task.Run(
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


[UsedImplicitly]
#pragma warning disable CA1050
public record Options
{
    [Option('i', "interval", Required = false, HelpText = "Interval of a messages in ms.")]
    public int Interval { get; set; } = 1000;

    [Option('c', "config", Required = false, HelpText = "The NLog.config")]
    public string NLogConfigFile { get; set; } = "NLog.console.config";

    [Option('s', "size", Required = false, HelpText = "Starting size of a message.")]
    public int Size { get; set; } = 30;

    [Option('a', "add", Required = false, HelpText = "Add 1 size to each N message.")]
    public int AddSize { get; set; } = 1;
    
    [Option('m', "message", Required = false, HelpText = "The message. Or at least a part of it.")]
    public string? Message { get; set; } = null;
}
