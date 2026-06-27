using System.IO.Compression;
using System.Text;

namespace Xtml.Runtime.Assets;

public static class AssetsHelper
{
    private static readonly Lazy<byte[]> Js = new(() => Load("ui.js"));
    private static readonly Lazy<byte[]> JsGzip = new(() => CompressGZip(Js.Value));
    private static readonly Lazy<byte[]> JsBr = new(() => CompressBrotli(Js.Value));
    private static readonly Lazy<byte[]> Css = new(() => Load("ui.css"));
    private static readonly Lazy<byte[]> CssGzip = new(() => CompressGZip(Css.Value));
    private static readonly Lazy<byte[]> CssBr = new(() => CompressBrotli(Css.Value));

    public static byte[] JS => Js.Value;
    public static byte[] JS_GZIP => JsGzip.Value;
    public static byte[] JS_BR => JsBr.Value;

    public static byte[] CSS => Css.Value;
    public static byte[] CSS_GZIP => CssGzip.Value;
    public static byte[] CSS_BR => CssBr.Value;

    public static (byte[] Body, string? ContentEncoding) GetJs(string? acceptEncoding)
    {
        if (acceptEncoding is not null)
        {
            if (Accepts(acceptEncoding, "br"))
                return (JS_BR, "br");
            if (Accepts(acceptEncoding, "gzip"))
                return (JS_GZIP, "gzip");
        }

        return (JS, null);
    }

    public static (byte[] Body, string? ContentEncoding) GetCss(string? acceptEncoding)
    {
        if (acceptEncoding is not null)
        {
            if (Accepts(acceptEncoding, "br"))
                return (CSS_BR, "br");
            if (Accepts(acceptEncoding, "gzip"))
                return (CSS_GZIP, "gzip");
        }

        return (CSS, null);
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
        Encoding.UTF8.GetBytes(new StreamReader(typeof(AssetsHelper).Assembly
            .GetManifestResourceStream($"{typeof(AssetsHelper).Assembly.GetName().Name}.Assets.{resourceName}")!
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