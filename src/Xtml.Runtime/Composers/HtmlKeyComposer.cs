using System.Buffers;
using System.Drawing;
using Xtml.Templating;
using Xtml.Templating.Composers;
using Xtml.Dom;
using Xtml.Runtime.Utilities;

namespace Xtml.Runtime.Composers;

public class HtmlKeyComposer(IBufferWriter<byte> writer, WindowBuilder window)
    : BaseKeyComposer, IStreamingComposer
{
    private enum AttributeStatus { None, Pending, InProgress }
    private AttributeStatus _attributeStatus = AttributeStatus.None;
    private ReadOnlyMemory<char>? _deferredLiteral = null;
    private bool _isHeadOmitted = false;

    public IBufferWriter<byte> Writer { get; set; } = writer;
    public WindowBuilder Window { get; set; } = window;

    public override bool OnTemplateBegin(ref Html html, ref string markup)
    {
        InjectKernel(ref markup);

        return true;
    }

    public override bool OnTemplateEnd(ref Html html)
    {
        if (_isHeadOmitted)
        {
            Writer.Write("""
                    
                </body>
            </html>
            """u8);
        }

        return true;
    }

    public override bool OnMarkup(ref Html parent, ref string literal, int relativeOrder = -1)
    {
        base.OnMarkup(ref parent, ref literal, relativeOrder);

        // This makes the assumption that keyholes preceded with an '=' are always attributes.  
        // Attributes need different sentinels than regular keyholes and boolean attributes 
        // have a few strange rules to follow:
        // https://developer.mozilla.org/en-US/docs/Glossary/Boolean/HTML
        if (literal.EndsWith('='))
        {
            _attributeStatus = AttributeStatus.Pending;
            _deferredLiteral = literal.AsMemory();
            return true;
        }

        Writer.Write(literal);

        return true;
    }

    public override bool OnStringKeyhole(ref Html parent, string value)
    {
        base.OnStringKeyhole(ref parent, value);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--key:{Key}-->{value}<!--/key:{Key}-->`
                Writer.Write("<!--key:"u8, Key, "-->"u8);
                Writer.Write(value);
                Writer.Write("<!--/key:"u8, Key, "-->"u8);
                break;

            case AttributeStatus.Pending:
                HandleDeferredLiteral();
                // ex: `"{value}" key:{Key}`
                Writer.Write("\""u8);
                Writer.Write(value);
                Writer.Write("\" key:"u8);
                Writer.Write(Key);
                // status jumps from .Pending to .None because the whole 
                // attribute is just one value, not a bunch of keyholes+literals.
                _attributeStatus = AttributeStatus.None;
                break;

            case AttributeStatus.InProgress:
                // No sentinels.  This keyhole is a part of a larger attribute
                // composed of multiple keyholes+literals.  Write only the value.
                Writer.Write(value);
                break;
        }

        return true;
    }

    public override bool OnBoolKeyhole(ref Html parent, bool value)
    {
        base.OnBoolKeyhole(ref parent, value);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--key:{Key}-->{b}<!--/key:{Key}-->`
                Writer.Write("<!--key:"u8, Key, "-->"u8);
                Writer.Write(value ? "true" : "false");
                Writer.Write("<!--/key:"u8, Key, "-->"u8);
                break;

            case AttributeStatus.Pending:
                var attributeName = HandleDeferredLiteral(isBooleanAttribute: true);
                if (value)
                {
                    // ex: ` {attributeName}`
                    Writer.Write(" "u8);
                    Writer.Write(attributeName);
                }
                // ex: ` key:{Key}="{attributeName}"`
                Writer.Write(" key:"u8);
                Writer.Write(Key);
                Writer.Write("=\""u8);
                Writer.Write(attributeName);
                Writer.Write("\""u8);

                // status jumps from .Pending to .None because the whole 
                // attribute is just one value, not a bunch of keyholes+literals.
                _attributeStatus = AttributeStatus.None;
                break;

            case AttributeStatus.InProgress:
                // No sentinels.  This keyhole is a part of a larger attribute
                // composed of multiple keyholes+literals.  Write only the value.
                Writer.Write(value ? "true" : "false");
                break;
        }

        return true;
    }

    public override bool OnIntKeyhole(ref Html parent, int value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnLongKeyhole(ref Html parent, long value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnFloatKeyhole(ref Html parent, float value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnDoubleKeyhole(ref Html parent, double value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnDecimalKeyhole(ref Html parent, decimal value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnDateTimeKeyhole(ref Html parent, DateTime value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnDateOnlyKeyhole(ref Html parent, DateOnly value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnTimeSpanKeyhole(ref Html parent, TimeSpan value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    public override bool OnTimeOnlyKeyhole(ref Html parent, TimeOnly value, string? format = null) => OnUtf8SpanFormattable(ref parent, value, format);
    private bool OnUtf8SpanFormattable<T>(ref Html parent, T value, string? format = null)
        where T : struct, IUtf8SpanFormattable
    {
        // Wraps the mutable value with two comment tags
        // to separate it from any neighboring text.
        // At the end of the body an inline script registers them 
        // because we can't rely on id= or document.getElementById().

        base.OnKeyhole(ref parent);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--key:{Key}-->{value:format}<!--/key:{Key}-->`
                Writer.Write("<!--key:"u8, Key, "-->"u8);
                Writer.Write(value, format);
                Writer.Write("<!--/key:"u8, Key, "-->"u8);
                break;

            case AttributeStatus.Pending:
                HandleDeferredLiteral();
                // ex: `"{value:format}" key:{Key}`
                Writer.Write("\""u8);
                Writer.Write(value, format);
                Writer.Write("\" key:"u8);
                Writer.Write(Key);
                // status jumps from .Pending to .None because the whole 
                // attribute is just one value, not a bunch of keyholes+literals.
                _attributeStatus = AttributeStatus.None;
                break;

            case AttributeStatus.InProgress:
                // No sentinels.  This keyhole is a part of a larger attribute
                // composed of multiple keyholes+literals.  Write only the value.
                Writer.Write(value, format);
                break;
        }

        return true;
    }

    public override bool OnColorKeyhole(ref Html parent, Color value, string? format = null)
    {
        base.OnColorKeyhole(ref parent, value, format);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--key:{Key}-->{value:format}<!--/key:{Key}-->`
                Writer.Write("<!--key:"u8, Key, "-->"u8);
                Writer.Write(value, format);
                Writer.Write("<!--/key:"u8, Key, "-->"u8);
                break;

            case AttributeStatus.Pending:
                HandleDeferredLiteral();
                // ex: `"{value:format}" key:{Key}`
                Writer.Write("\""u8);
                Writer.Write(value, format);
                Writer.Write("\" key:"u8);
                Writer.Write(Key);
                // status jumps from .Pending to .None because the whole 
                // attribute is just one value, not a bunch of keyholes+literals.
                _attributeStatus = AttributeStatus.None;
                break;

            case AttributeStatus.InProgress:
                // No sentinels.  This keyhole is a part of a larger attribute
                // composed of multiple keyholes+literals.  Write only the value.
                Writer.Write(value, format);
                break;
        }

        return true;
    }

    public override bool OnUriKeyhole(ref Html parent, Uri value, string? format = null)
        => OnStringKeyhole(ref parent, value.ToString()); // TODO: Memory allocation!
        
    public override bool OnHtmlBegin(ref Html html, int relativeOrder = -1)
    {
        base.OnHtmlBegin(ref html, relativeOrder);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--key:{Key}-->`
                Writer.Write("<!--key:"u8, Key, "-->"u8);
                break;
            case AttributeStatus.Pending:
                HandleDeferredLiteral();
                // ex: `"` (the value will come later in the next On*Keyhole())
                Writer.Write("\""u8);
                _attributeStatus = AttributeStatus.InProgress;
                break;
        }

        return true;
    }

    public override bool OnHtmlEnd(ref Html parent, scoped Html html, int relativeOrder = -1, string? transition = null, string? expression = null)
    {
        base.OnHtmlEnd(ref parent, html, relativeOrder, transition, expression);

        switch (_attributeStatus)
        {
            case AttributeStatus.None:
                // ex: `<!--/key:{Key}-->`
                Writer.Write("<!--/key:"u8, Key, "-->"u8);
                if (transition is {} trns)
                    InjectTransition(trns);
                break;

            case AttributeStatus.InProgress:
                // ex: `" key:{Key}`
                Writer.Write("\" key:"u8);
                Writer.Write(Key);
                _attributeStatus = AttributeStatus.None;
                break;

            case AttributeStatus.Pending:
                throw new NotSupportedException("Attributes cannot have nested Htmls");
        }

        return true;
    }

    public override bool OnIteratorBegin(ref Html parent, ref Html htmls, string? transition = null, string? expression = null)
    {
        base.OnIteratorBegin(ref parent, ref htmls, transition, expression);
        return true;
    }

    public override bool OnIteratorEnd(ref Html parent, ref Html htmls, string? transition = null, string? expression = null)
    {
        base.OnIteratorEnd(ref parent, ref htmls, transition, expression);
        
        // Keyhole to represent the loop itself, useful for zero-length use cases.
        // ex: `<!--key:{Key} /-->`
        Writer.Write("<!--key:"u8, Key, " /-->"u8);

        return true;
    }

    public override bool OnListener(ref Html parent, Action listener, string? trim = null, string? expression = null) => OnListener(ref parent, includeEventArg: false, trim);
    public override bool OnListener(ref Html parent, Action<Event> listener, string? trim = null, string? expression = null) => OnListener(ref parent, includeEventArg: true, trim);
    public override bool OnListener(ref Html parent, Func<Task> listener, string? trim = null, string? expression = null) => OnListener(ref parent, includeEventArg: false, trim);
    public override bool OnListener(ref Html parent, Func<Event, Task> listener, string? trim = null, string? expression = null) => OnListener(ref parent, includeEventArg: true, trim);
    private bool OnListener(ref Html parent, bool includeEventArg, string? format = null)
    {
        base.OnKeyhole(ref parent);

        if (_deferredLiteral != null)
            HandleDeferredLiteral();

        if (!includeEventArg)
        {
            // ex: `"keyholes['1:2:3'].dispatchEvent(event)" key:1:2:3`
            Writer.Write("\"keyholes['"u8);
            Writer.Write(Key);
            Writer.Write("'].dispatchEvent(event)\" key:"u8);
            Writer.Write(Key);
        }
        else if (format is not null)
        {
            // ex: `"keyholes['1:2:3'].dispatchEvent(event,'x,y'))" key:1:2:3`
            Writer.Write("\"keyholes['"u8);
            Writer.Write(Key);
            Writer.Write("'].dispatchEvent(event,'"u8);
            Writer.Write(format);
            Writer.Write("')\" key:"u8);
            Writer.Write(Key);
        }
        else if (format is null)
        {
            // ex: `"keyholes['1:2:3'].dispatchEvent(event,'*'))" key:1:2:3`
            Writer.Write("\"keyholes['"u8);
            Writer.Write(Key);
            Writer.Write("'].dispatchEvent(event,'*')\" key:"u8);
            Writer.Write(Key);
        }
        
        _attributeStatus = AttributeStatus.None;
        return true;
    }

    private void HandleDeferredLiteral()
    {
        if (!_deferredLiteral.HasValue)
            throw new NullReferenceException(nameof(_deferredLiteral));

        Writer.Write(_deferredLiteral.Value);
        _deferredLiteral = null;
    }

    private ReadOnlySpan<char> HandleDeferredLiteral(bool isBooleanAttribute = true)
    {
        if (!isBooleanAttribute)
        {
            HandleDeferredLiteral();
            return [];
        }

        if (!_deferredLiteral.HasValue)
            throw new NullReferenceException(nameof(_deferredLiteral));

        // This string literal will look something like `...<input type="checkbox" checked=`
        // Note: We know they always end with `=`.
        var deferredLiteralSpan = _deferredLiteral.Value.Span;
        int indexBeforeAttribute = deferredLiteralSpan.LastIndexOf(' ');
        ArgumentOutOfRangeException.ThrowIfLessThan(indexBeforeAttribute, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(indexBeforeAttribute, deferredLiteralSpan.Length - 2);

        Writer.Write(deferredLiteralSpan[..indexBeforeAttribute]);
        var attributeName = deferredLiteralSpan[(indexBeforeAttribute + 1)..^1];

        _deferredLiteral = null;
        return attributeName;
    }

    private void InjectKernel(ref string literal)
    {
        int headIndex = literal.IndexOf("<head>", StringComparison.Ordinal);
        _isHeadOmitted = headIndex < 0;
        headIndex += 6;

        if (_isHeadOmitted)
        {
            Writer.Write("""
            <!doctype html>
            <html>
                <head>

            """u8);
        }
        else
        {
            Writer.Write(literal.AsSpan(..headIndex));
        }

        Writer.Write("""

                <!-- Injected by XTML -->
                <script src="/_app/websocket/ui.js" defer></script>
                <link href="/_app/base/ui.css" rel="stylesheet" />
                <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
                <meta charset="UTF-8">

        """u8);

        // Write event handlers set on window or document
        if (Window.Listeners.Count > 0)
        {
            Writer.Write("\n\n<script>\n"u8);

            foreach (var listener in Window.Listeners)
            {
                // ex: `  {listener.Html}\n`
                Writer.Write("  "u8);
                Writer.Write(listener.Html ?? "");
                Writer.Write("\n"u8);
            }

            Writer.Write("</script>\n\n"u8);
        }

        if (_isHeadOmitted)
        {
            Writer.Write("""

                </head>
                <body>

            """u8);
        }
        else
        {
            // Pre-handle the work of OnMarkup, except consider `offset`.
            // Then set `literal` to "" so the next OnMarkup no-ops.
            int offset = _isHeadOmitted ? 0 : headIndex;
            if (literal.EndsWith('='))
            {
                _attributeStatus = AttributeStatus.Pending;
                _deferredLiteral = literal.AsMemory(offset);
            }
            else
            {
                Writer.Write(literal.AsSpan(offset));
            }
        }

        literal = string.Empty;
    }

    private void InjectTransition(string transition)
    {
        Span<byte> key = stackalloc byte[Key.Length];
        for (int i = 0; i < key.Length; i++)
            key[i] = Key[i] == ':' ? (byte)'-' : Key[i];
        Writer.WriteRaw($$"""
            <style>
                ::view-transition-group(xtml-fwd-{{key}}, xtml-rev-{{key}}) { animation: none; }
                ::view-transition-new(xtml-fwd-{{key}}) { width: auto; height: auto; animation: 300ms ease-in-out {{transition}}-in; }
                ::view-transition-old(xtml-fwd-{{key}}) { width: auto; height: auto; animation: 300ms ease-in-out {{transition}}-out; }
                ::view-transition-new(xtml-rev-{{key}}) { width: auto; height: auto; animation: 300ms ease-in-out {{transition}}-out reverse; }
                ::view-transition-old(xtml-rev-{{key}}) { width: auto; height: auto; animation: 300ms ease-in-out {{transition}}-in reverse; }
            </style>
            """);
    }

    public override void Reset()
    {
        Writer = null!;
        Window = null!;
        _attributeStatus = AttributeStatus.None;
        base.Reset();
    }

    [ThreadStatic] static HtmlKeyComposer? reusable;
    public static HtmlKeyComposer Reuse(IBufferWriter<byte> writer, WindowBuilder window) 
    {
        if (reusable is {} composer)
        {
            composer.Writer = writer;
            composer.Window = window;
            return composer;
        }

        return reusable = new HtmlKeyComposer(writer, window);
    }
}