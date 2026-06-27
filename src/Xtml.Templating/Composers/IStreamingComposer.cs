using System.Buffers;

namespace HtmlString.Composers;

public interface IStreamingComposer
{
    public IBufferWriter<byte> Writer { get; set; }
}