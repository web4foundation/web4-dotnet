using System.Buffers;
using System.Threading.Channels;
using Keyholes;
using Keyholes.Utilities;

namespace Web4.WebSocket.Buffers;

partial class JsonRpcWriter
{
    private class FlushOnAwait : SynchronizationContext
    {
        private record struct StateHolder(SendOrPostCallback Callback, object? State, FlushOnAwait FlushOnAwait);
        public ChannelWriter<ReadOnlySequence<byte>>? Flusher { get; set; }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            using var perf = Perf.Measure("FlushOnAwait.Post"); // TODO: Remove PerfCheck

            ThreadPool.QueueUserWorkItem<StateHolder>(
                callBack: static s =>
                {
                    // Set it again because AspNetWebSocket uses ConfigureAwait(false)
                    SetSynchronizationContext(s.FlushOnAwait);

                    s.Callback(s.State);

                    var flusher = s.FlushOnAwait.Flusher ?? throw new NullReferenceException("🛑 Cannot use FlushOnAwait without a Flusher or expect the system to get into a bad state.  Needs investigating.");
                    var writer = JsonRpcWriter.Current(flusher);
                    writer.Flush();
                },
                state: new(callback, state, this),
                preferLocal: false
            );
        }
    }
}