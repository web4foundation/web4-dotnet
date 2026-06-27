namespace HtmlString;

public static partial class Extensions
{
    // TODO: Implement format strings for Uri
    public static bool TryFormat(this Uri uri, Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format)
    {
        bytesWritten = 0;
        return true;
    }
}
