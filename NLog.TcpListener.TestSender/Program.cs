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
              Console.WriteLine("Press any key to stop");
              Task.Run(
                  async () =>
                  {
                      var currentRow = 0u;
                      while (!cts.Token.IsCancellationRequested)
                      {
                          var i = unchecked(++currentRow);
                          if (i % o.IncrementLength == 0)
                          {
                              ++o.Length;
                          }

                          var message = "{row}: " + new string('.', o.Length);
                          // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                          logger.Log(LogLevel.FromOrdinal(((int)i - 1) % 7), message, i);
                          await Task.Delay(o.Interval, cts.Token);
                      }
                  });

              Console.ReadKey();
              cts.Cancel();
              Console.WriteLine("Finished!");
          });


[UsedImplicitly]
#pragma warning disable CA1050
public record Options
{
    [Option('i', "interval", Required = false, HelpText = "Interval of a messages in ms.")]
    public int Interval { get; set; } = 1000;

    [Option('c', "config", Required = false, HelpText = "The NLog.config")]
    public string NLogConfigFile { get; set; } = "NLog.console.config";

    [Option('l', "length", Required = false, HelpText = "Length of a message.")]
    public int Length { get; set; } = 30;

    [Option("incrementlenght", Required = false, HelpText = "Increment the length of a message each N message by 1.")]
    public int IncrementLength { get; set; } = 1;
}
