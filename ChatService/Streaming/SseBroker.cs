using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Workflow.ChatService.Streaming
{
    public class SseBroker
    {
        private readonly ConcurrentDictionary<string, Channel<string>> _clients = new();

        public (string clientId, ChannelReader<string> reader) Subscribe()
        {
            var id = Guid.NewGuid().ToString("N");
            var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _clients[id] = channel;
            return (id, channel.Reader);
        }

        public void Unsubscribe(string clientId)
        {
            if (_clients.TryRemove(clientId, out var channel))
                channel.Writer.TryComplete();
        }

        public void Publish(string payload)
        {
            foreach (var kv in _clients)
                kv.Value.Writer.TryWrite(payload);
        }
    }
}
