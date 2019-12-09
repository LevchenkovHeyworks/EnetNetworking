using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ENet;

namespace Hollywood.Networking.Server.Console
{
    public class EnetServer
    {
        private const int MaxClients = 1000;
        private const int ServerTickRate = 64;

        private readonly ushort port;
        private readonly NetworkingChannel networkingChannel;

        private Host server;
        private CancellationTokenSource cancellationTokenSource;

        public EnetServer(ushort port, NetworkingChannel networkingChannel)
        {
            this.port = port;
            this.networkingChannel = networkingChannel;
        }

        public Task Start()
        {
            cancellationTokenSource = new CancellationTokenSource();

            server = new Host();
            var address = new Address();
            address.Port = port;
            server.Create(address, MaxClients, 4);

            Task.Run(() => RunLoop(server, cancellationTokenSource.Token));
            
            return Task.CompletedTask;
        }

        private void RunLoop(Host server, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                server.Service(1000 / ServerTickRate, out Event netEvent);

                switch (netEvent.Type)
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        System.Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        if (!networkingChannel.Writer.TryWrite(new PeerConnectedMessage(netEvent.Peer)))
                        {
                            System.Console.WriteLine("Message cann't be written to the channel.");
                        }
                        break;

                    case EventType.Disconnect:
                        System.Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        if (!networkingChannel.Writer.TryWrite(new PeerDisconnectedMessage(netEvent.Peer)))
                        {
                            System.Console.WriteLine("Message cann't be written to the channel.");
                        }
                        break;

                    case EventType.Timeout:
                        System.Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        if (!networkingChannel.Writer.TryWrite(new TimeoutMessage(netEvent.Peer)))
                        {
                            System.Console.WriteLine("Message cann't be written to the channel.");
                        }
                        break;

                    case EventType.Receive:

                        System.Console.WriteLine("Server receive message - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        //var data = ArrayPool<byte>.Shared.Rent(netEvent.Packet.Length);
                        //netEvent.Packet.CopyTo(data);

                        byte[] array;
                        unsafe
                        {
                            var span = new Span<byte>(netEvent.Packet.Data.ToPointer(), netEvent.Packet.Length);
                            array = span.ToArray();
                        }

                        if (!networkingChannel.Writer.TryWrite(new PacketReceivedMessage(netEvent.Peer, array, netEvent.Packet.Length)))
                        {
                            System.Console.WriteLine("Message cann't be written to the channel.");
                        }

                        netEvent.Packet.Dispose();

                        break;
                }
            }
        }

        private static void SendReliable(byte[] data, byte channelId, Peer peer)
        {
            Packet packet = default(Packet);

            packet.Create(data, data.Length, PacketFlags.Reliable | PacketFlags.NoAllocate); // Reliable Sequenced
            peer.Send(channelId, ref packet);
        }

        private static void SendUnreliable(byte[] data, byte channelId, Peer peer)
        {
            Packet packet = default(Packet);

            packet.Create(data, data.Length, PacketFlags.None | PacketFlags.NoAllocate); // Unreliable Sequenced
            peer.Send(channelId, ref packet);
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();
            server?.Dispose();
            server = null;
        }
    }
}