namespace Web4.WebSocket.Buffers;

partial class JsonRpcWriter
{
    public struct FlushOnDispose(JsonRpcWriter writer, bool continueOnCapturedContext = false) : IDisposable
    {
        public void Dispose()
        {
            writer.Flush();
            if (continueOnCapturedContext)
                SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}