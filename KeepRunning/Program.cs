using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KeepRunning
{
    public class Program
    {
        private static bool _running = false;
        private static Process _process;

        static Task Main(string[] args)
        {
            return new Program().MainAsync();
        }

        private Program()
        {
            _running = true;
            _process = Process.Start("../../../../EMBRS/bin/Release/net5.0/EMBRS.exe");
            Console.WriteLine($"{DateTime.Now,-19} [Info]: Loaded EMBRS");
        }

        private async Task MainAsync()
        {
            var loopTask = Task.Run(async () =>
            {
                while (_running)
                {
                    await LoopTasks();
                    await Task.Delay(1000);
                }
            });

            await Task.Delay(Timeout.Infinite);
        }

        private async Task LoopTasks()
        {
            try
            {
                if (_process.HasExited)
                {
                    _process = Process.Start("../../../../EMBRS/bin/Release/net5.0/EMBRS.exe");
                    Console.WriteLine($"{DateTime.Now,-19} [Info]: Reloaded EMBRS");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now,-19} [Error] {ex.Source}: {ex.Message} {ex}");
            }
        }
    }
}
