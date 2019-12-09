using System;
using System.Threading.Tasks;

namespace Hollywood.Networking.Client.Console
{
    class Program
    {
        private const ushort port = 33445;
        private const string ip = "127.0.0.1";

        static async Task Main(string[] args)
        {
            ENet.Library.Initialize();
            var client = new EnetClient(ip, port);
            await client.Start();

            bool isRunned = true;
            while (isRunned)
            {
                System.Console.WriteLine("Input message:");
                var message = System.Console.ReadLine();

                if (message == "q")
                {
                    isRunned = false;
                }
                else
                {
                    client.Send(message);
                }
            }

            await client.Stop();
            ENet.Library.Deinitialize();
        }

    }
}
