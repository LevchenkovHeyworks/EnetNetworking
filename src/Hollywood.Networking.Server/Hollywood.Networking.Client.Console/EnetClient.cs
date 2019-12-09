using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ENet;

namespace Hollywood.Networking.Client.Console
{
    public class EnetClient
    {
        private const int ClientTickRate = 64;

        private readonly ushort port;
        private readonly string ip;
        private Host client;
        private Peer peer;

        private CancellationTokenSource cancellationTokenSource;

        public EnetClient(string ip, ushort port)
        {
            this.port = port;
            this.ip = ip;
            
        }

        public Task Start()
        {
            cancellationTokenSource = new CancellationTokenSource();

            client = new Host();
            Address address = new Address();

            address.SetHost(ip);
            address.Port = port;
            client.Create();

            peer = client.Connect(address, 4);
            
            Task.Run(() => RunLoop(cancellationTokenSource.Token));
            
            return Task.CompletedTask;
        }

        private void RunLoop(CancellationToken cancellationToken)
        {
            Event netEvent;
            while (!cancellationToken.IsCancellationRequested)
            {
                client.Service(1000 / ClientTickRate, out netEvent);

                switch (netEvent.Type)
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        System.Console.WriteLine("Client connected to server");
                        break;

                    case EventType.Disconnect:
                        System.Console.WriteLine("Client disconnected from server");
                        break;

                    case EventType.Timeout:
                        System.Console.WriteLine("Client connection timeout");
                        break;

                    case EventType.Receive:
                        System.Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);

                        var data = ArrayPool<byte>.Shared.Rent(netEvent.Packet.Length);
                        netEvent.Packet.CopyTo(data);
                        var message = Encoding.UTF8.GetString(data);
                        System.Console.WriteLine("Echo from server: " + message);

                        netEvent.Packet.Dispose();
                        break;
                }
            }
        }

        public void Send(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            Packet packet = default(Packet);
            packet.Create(data);
            peer.Send(0, ref packet);
        }

        public Task Stop()
        {
            client.Flush();
            client.Dispose();
            return Task.CompletedTask;
        }
    }
}