using System;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private static async Task Execute()
        {
            var host = Server.Create();
            await host.StartAsync();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            await host.StopAsync();
        }

        private static void Main(string[] args)
        {
            Execute().Wait();
        }
    }
}
