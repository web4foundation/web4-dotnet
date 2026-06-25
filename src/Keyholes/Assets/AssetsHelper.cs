using System.IO.Compression;
using System.Text;

namespace Keyholes.Assets;

public static class AssetsHelper
{
    private static readonly Lazy<byte[]> Web4Js = new(() => Load("Keyholes.Assets.web4.js"));
    private static readonly Lazy<byte[]> Web4JsGzip = new(() => CompressGZip(Web4Js.Value));
    private static readonly Lazy<byte[]> Web4JsBr = new(() => CompressBrotli(Web4Js.Value));

    private static readonly Lazy<byte[]> Web4Css = new(() => Load("Keyholes.Assets.web4.css"));
    private static readonly Lazy<byte[]> Web4CssGzip = new(() => CompressGZip(Web4Css.Value));
    private static readonly Lazy<byte[]> Web4CssBr = new(() => CompressBrotli(Web4Css.Value));

    public static byte[] WEB4_JS => Web4Js.Value;
    public static byte[] WEB4_JS_GZIP => Web4JsGzip.Value;
    public static byte[] WEB4_JS_BR => Web4JsBr.Value;

    public static byte[] WEB4_CSS => Web4Css.Value;
    public static byte[] WEB4_CSS_GZIP => Web4CssGzip.Value;
    public static byte[] WEB4_CSS_BR => Web4CssBr.Value;

    public static (byte[] Body, string? ContentEncoding) GetWeb4Js(string? acceptEncoding)
    {
        if (acceptEncoding is not null)
        {
            if (Accepts(acceptEncoding, "br"))
                return (WEB4_JS_BR, "br");
            if (Accepts(acceptEncoding, "gzip"))
                return (WEB4_JS_GZIP, "gzip");
        }

        return (WEB4_JS, null);
    }

    public static (byte[] Body, string? ContentEncoding) GetWeb4Css(string? acceptEncoding)
    {
        if (acceptEncoding is not null)
        {
            if (Accepts(acceptEncoding, "br"))
                return (WEB4_CSS_BR, "br");
            if (Accepts(acceptEncoding, "gzip"))
                return (WEB4_CSS_GZIP, "gzip");
        }

        return (WEB4_CSS, null);
    }

    private static bool Accepts(string acceptEncoding, string encoding)
    {
        foreach (var part in acceptEncoding.Split(','))
        {
            var token = part.AsSpan().Trim();
            var semicolon = token.IndexOf(';');
            if (semicolon >= 0)
                token = token[..semicolon].Trim();
            if (token.Equals(encoding, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static byte[] Load(string resourceName) =>
        Encoding.UTF8.GetBytes(new StreamReader(System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(resourceName)!
        ).ReadToEnd());

    private static byte[] CompressGZip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize))
            gzip.Write(data);
        return output.ToArray();
    }

    private static byte[] CompressBrotli(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize))
            brotli.Write(data);
        return output.ToArray();
    }
}