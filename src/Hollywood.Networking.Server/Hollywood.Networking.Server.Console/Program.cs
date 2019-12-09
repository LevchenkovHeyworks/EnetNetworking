using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ENet;

namespace Hollywood.Networking.Server.Console
{
    class Program
    {
        private static List<Peer> peers;

        private const ushort port = 33445;
        static async Task Main(string[] args)
        {
            ENet.Library.Initialize();
            peers = new List<Peer>();

            var networkingChannel = new NetworkingChannel();
            Task.Run(() => ReadMessages(networkingChannel));

            var server = new EnetServer(port, networkingChannel);
            await server.Start();

            System.Console.WriteLine("Press any key to stop server.");
            System.Console.ReadKey();
            networkingChannel.Stop();
            await server.Stop();
            ENet.Library.Deinitialize();
        }

        private static async Task ReadMessages(NetworkingChannel channel)
        {
            while (!channel.CancellationTokenSource.IsCancellationRequested && await channel.Reader.WaitToReadAsync(channel.CancellationTokenSource.Token))
            {
                while (!channel.CancellationTokenSource.IsCancellationRequested && channel.Reader.TryRead(out var message))
                {
                    try
                    {
                        if (message is PeerConnectedMessage connectedMessage)
                        {
                            peers.Add(connectedMessage.Peer);
                        }
                        else if (message is PeerDisconnectedMessage disconnectedMessage)
                        {
                            peers.RemoveAll(x => x.ID == disconnectedMessage.Peer.ID);
                        }
                        else if (message is PacketReceivedMessage receivedMessage)
                        {
                            foreach (var peer in peers)
                            {
                                Packet packet = default(Packet);
                                packet.Create(receivedMessage.Data, receivedMessage.Length);
                                peer.Send(1, ref packet);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }
            }
        }
    }
}
