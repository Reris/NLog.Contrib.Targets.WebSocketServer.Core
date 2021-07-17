using NLog;
using System;
using System.Threading.Tasks;

namespace ConsoleApplicationCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = LogManager.GetCurrentClassLogger();

            var end = false;
            var task = Task.Run(async () =>
            {
                while (!end)
                {
                    log.Fatal("This is a fatal.");
                    log.Error("This is an error.");
                    log.Warn("This is a warning.");
                    log.Info("This is information.");

                    await Task.Delay(1000);
                }
            });

            Console.ReadKey(true);
            end = true;
            task.Wait();
            Console.WriteLine("END");
        }
    }
}
