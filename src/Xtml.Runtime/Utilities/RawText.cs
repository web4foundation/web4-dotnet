#pragma warning disable CS9113 // Parameter is unread.
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Keyholes.Utilities;

[InterpolatedStringHandler]
[StructLayout(LayoutKind.Auto)]
public readonly ref struct RawText(int literalLength, int formattedCount, IBufferWriter<byte> writer)
{
    public readonly void AppendLiteral(ReadOnlySpan<char> value) => Append(value);
    public readonly void AppendFormatted(ReadOnlySpan<char> value) => Append(value);
    private readonly void Append(ReadOnlySpan<char> value)
    {
        Span<byte> buffer = writer.GetSpan(value.Length);
        int length = Encoding.UTF8.GetBytes(value, buffer);
        writer.Advance(length);
    }

    public readonly void AppendFormatted(Span<byte> value)
    {
        Span<byte> buffer = writer.GetSpan(value.Length);
        value.CopyTo(buffer);
        writer.Advance(value.Length);
    }

    public readonly void AppendFormatted<T>(T value, string? format = null) where T : struct, IUtf8SpanFormattable
    {
        // TODO: What's the max length of all T's?
        const int tMaxLength = 128;

        Span<byte> buffer = writer.GetSpan(tMaxLength);
        value.TryFormat(buffer, out int length, format, null);
        writer.Advance(length);
    }
}

public static class RawTextExtensions
{
    public static void WriteRaw(
        this IBufferWriter<byte> writer,
        [InterpolatedStringHandlerArgument("writer")] ref RawText text)
    {
    }
}

#pragma warning restore CS9113 // Parameter is unread.