using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client("http://localhost:8080/", 1024);

            Task.Run(client.Download);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            client.Stop();
        }
    }
}
