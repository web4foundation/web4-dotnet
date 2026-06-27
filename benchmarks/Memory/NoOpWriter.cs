using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;

namespace Xtml.Composers;

public class NoOpWriter : IBufferWriter<byte>
{
    private static byte[] scratch = new byte[4096];

    public void Advance(int count)
    {
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (scratch.Length < sizeHint)
        {
            Console.WriteLine($"Too small.  Growing to {sizeHint}...");
            scratch = new byte[sizeHint];
        }
        return scratch.AsMemory();
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (scratch.Length < sizeHint)
        {
            Console.WriteLine($"Too small.  Growing to {sizeHint}...");
            scratch = new byte[sizeHint];
        }
        return scratch.AsSpan();
    }
}