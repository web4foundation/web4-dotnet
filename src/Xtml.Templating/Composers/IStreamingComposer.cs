using System.Buffers;

namespace Xtml.Templating.Composers;

public interface IStreamingComposer
{
    public IBufferWriter<byte> Writer { get; set; }
}