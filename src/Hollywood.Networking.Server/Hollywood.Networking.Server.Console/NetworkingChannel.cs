using System.Threading;
using System.Threading.Channels;
using ENet;

namespace Hollywood.Networking.Server.Console
{
    public class NetworkingChannel
    {
        private readonly Channel<INetworkingMessage> channel;

        public NetworkingChannel()
        {
            channel = Channel.CreateUnbounded<INetworkingMessage>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });

            Writer = channel.Writer;
            Reader = channel.Reader;

            CancellationTokenSource = new CancellationTokenSource();
        }

        public ChannelWriter<INetworkingMessage> Writer { get; }
        public ChannelReader<INetworkingMessage> Reader { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        public void Stop()
        {
            channel.Writer.TryComplete();
            channel.Reader.Completion.ContinueWith(task => CancellationTokenSource.Cancel());
        }
    }

    public interface INetworkingMessage
    {
        ref Peer Peer { get; }
    }

    public class PeerConnectedMessage : INetworkingMessage
    {
        private Peer peer;
        public PeerConnectedMessage(in Peer peer)
        {
            this.peer = peer;
        }

        public ref Peer Peer => ref peer;
    }

    public class PeerDisconnectedMessage : INetworkingMessage
    {
        private Peer peer;
        public PeerDisconnectedMessage(in Peer peer)
        {
            this.peer = peer;
        }

        public ref Peer Peer => ref peer;
    }

    public class PacketReceivedMessage : INetworkingMessage
    {
        private Peer peer;

        public PacketReceivedMessage(in Peer peer, byte[] data, int length)
        {
            this.peer = peer;
            this.Data = data;
            Length = length;
        }

        public ref Peer Peer => ref peer;

        public byte[] Data { get; }
        public int Length { get; }
    }

    public class TimeoutMessage : INetworkingMessage
    {
        private Peer peer;
        public TimeoutMessage(in Peer peer)
        {
            this.peer = peer;
        }

        public ref Peer Peer => ref peer;
    }
}
