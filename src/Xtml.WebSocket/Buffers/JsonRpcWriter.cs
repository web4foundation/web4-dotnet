using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;
using HtmlString;
using Keyholes;

namespace Web4.WebSocket.Buffers;

public partial class JsonRpcWriter : IDisposable
{
    // OK to relax escaping since WebSockets are ALWAYS UTF-8 (when not binary).
    // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding
    private readonly static JsonWriterOptions options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    [ThreadStatic]
    private static JsonRpcWriter? _threadStaticWriter;
    private readonly PooledSequenceBufferWriter<byte> _bufferWriter;
    private readonly Utf8JsonWriter _jsonWriter;
    private ChannelWriter<ReadOnlySequence<byte>>? _flusher = null;
    private FlushOnAwait? _flushOnAwait;
    private bool _isBatch = false;

    private JsonRpcWriter()
    {
        _bufferWriter = new();
        _jsonWriter = new(_bufferWriter, options);
    }

    public static JsonRpcWriter Current(ChannelWriter<ReadOnlySequence<byte>> flusher)
    {
        var writer = _threadStaticWriter ??= new();
        writer._flusher = flusher;

        if (SynchronizationContext.Current is FlushOnAwait)
            writer._isBatch = true;

        return writer;
    }

    public FlushOnDispose BatchThisScope(bool continueOnCapturedContext = false)
    {
        if (continueOnCapturedContext)
        {
            _flushOnAwait ??= new();
            _flushOnAwait.Flusher = _flusher;
            SynchronizationContext.SetSynchronizationContext(_flushOnAwait);
        }

        if (!_isBatch)
        {
            if (_bufferWriter.WrittenCount > 0)
                throw new InvalidOperationException("Cannot switch to batch.  Buffer already written to.");
            _isBatch = true;
        }

        return new FlushOnDispose(this, continueOnCapturedContext);
    }

    public void Flush()
    {
        _jsonWriter.Flush();

        if (_isBatch && _bufferWriter.WrittenCount > 0)
        {
            _jsonWriter.WriteEndArray();
            _jsonWriter.Flush();
        }

        _isBatch = false;

        if (_bufferWriter.WrittenCount == 0)
            return;

        var buffer = _bufferWriter.Sequence;
        _jsonWriter.Reset(_bufferWriter);

        if (_flusher is null)
            throw new InvalidOperationException("🛑 Trying to flush when flusher is null.  This should be impossible.  Needs investigating.");
        while (!_flusher.TryWrite(buffer)) ;
    }

    public void WriteNotification(string method)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");
        _jsonWriter.WriteString("method", method);
        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    public void WriteNotification<T>(string method, T param1)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WriteString("method", method);

        _jsonWriter.WriteStartArray("params");
        WriteTValue(param1);
        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    public void WriteNotification(string method, string param1, params Span<string> @params)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WriteString("method", method);

        _jsonWriter.WriteStartArray("params");
        _jsonWriter.WriteStringValue(param1);
        foreach (var param in @params)
            _jsonWriter.WriteStringValue(param);
        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    public void WriteNotification(string method, params Span<object> @params)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WriteString("method", method);

        _jsonWriter.WriteStartArray("params");
        foreach (var param in @params)
            _jsonWriter.WriteStringValue(param.ToString());
        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from SetValue
    public void WriteNotification(ValueTuple<string, byte[], string> method, ref Keyhole param1)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        if (param1.Type == KeyholeType.Boolean)
        {
            // HTML treats boolean attributes differently.  Send without quotes.
            _jsonWriter.WriteBooleanValue(param1.Boolean);
        }
        else
        {
            _jsonWriter.WriteStringValueSegment("", false);
            WriteMutableKeyholeValue(ref param1);
            _jsonWriter.WriteStringValueSegment("", true);
        }

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from SetValue
    public void WriteNotification(ValueTuple<string, byte[], string> method, Span<Keyhole> param1)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        WriteAttributeSequence(param1);
        _jsonWriter.WriteStringValueSegment("", true);

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from SetNode
    public void WriteNotification(Keyhole[] buffer, ValueTuple<string, byte[], string> method, Span<Keyhole> param1, ValueTuple<string, byte[]>? param2 = null)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        WriteHtml(buffer, param1, includeSentinels: true);
        _jsonWriter.WriteStringValueSegment("", true);

        if (param2.HasValue)
        {
            _jsonWriter.WriteStringValueSegment(param2.Value.Item1, false);
            WriteKey(method.Item2);
            _jsonWriter.WriteStringValueSegment("", true);
        }

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from SetNode
    public void WriteNotification(Keyhole[] buffer, ValueTuple<string, byte[], string> method, Span<Keyhole> param1, ValueTuple<string, int> param2, ValueTuple<string, int> param3)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        WriteHtml(buffer, param1, includeSentinels: true);
        _jsonWriter.WriteStringValueSegment("", true);

        Span<char> strInt = stackalloc char[11]; // max int length

        _jsonWriter.WriteStringValueSegment(param2.Item1, false);
        if (param2.Item2.TryFormat(strInt, out int length))
            _jsonWriter.WriteStringValueSegment(strInt[..length], false);
        _jsonWriter.WriteStringValueSegment("", true);

        _jsonWriter.WriteStringValueSegment(param3.Item1, false);
        if (param3.Item2.TryFormat(strInt, out length))
            _jsonWriter.WriteStringValueSegment(strInt[..length], false);
        _jsonWriter.WriteStringValueSegment("", true);

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from PushNode
    public void WriteNotification(Keyhole[] buffer, ValueTuple<string, byte[], string> method, Span<Keyhole> param1, byte[] param2, ValueTuple<string, int>? param3 = null)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        WriteHtml(buffer, param1, includeSentinels: true);
        _jsonWriter.WriteStringValueSegment("", true);

        _jsonWriter.WriteStringValue(param2);

        if (param3 is not null)
        {
            _jsonWriter.WriteStringValueSegment(param3.Value.Item1, false);
            Span<char> strInt = stackalloc char[11]; // max int length
            if (param3.Value.Item2.TryFormat(strInt, out int length))
                _jsonWriter.WriteStringValueSegment(strInt[..length], false);
            _jsonWriter.WriteStringValueSegment("", true);
        }

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    // Called from PopNode
    public void WriteNotification(ValueTuple<string, byte[], string> method, ValueTuple<string, int>? param1 = null)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");

        _jsonWriter.WritePropertyName("method");
        _jsonWriter.WriteStringValueSegment(method.Item1, false);
        WriteKey(method.Item2);
        _jsonWriter.WriteStringValueSegment(method.Item3, true);

        _jsonWriter.WriteStartArray("params");

        if (param1.HasValue)
        {
            _jsonWriter.WriteStringValueSegment(param1.Value.Item1, false);
            Span<char> strInt = stackalloc char[11]; // max int length
            if (param1.Value.Item2.TryFormat(strInt, out int length))
                _jsonWriter.WriteStringValueSegment(strInt[..length], false);
            _jsonWriter.WriteStringValueSegment("", true);
        }

        _jsonWriter.WriteEndArray();

        _jsonWriter.WriteEndObject();

        OnMessageEnd();
    }

    private void WriteKey(byte[] key)
    {
        _jsonWriter.Flush();
        key.CopyTo(_bufferWriter.GetSpan(key.Length));
        _bufferWriter.Advance(key.Length);
    }

    private void WriteHtml(Keyhole[] buffer, Span<Keyhole> keyholes, bool includeSentinels)
    {
        for (int i = 0; i < keyholes.Length; i++)
        {
            ref var keyhole = ref keyholes[i];

            switch (keyhole.Type)
            {
                case KeyholeType.StringLiteral:
                    _jsonWriter.WriteStringValueSegment(keyhole.StringLiteral, false);
                    break;
                case KeyholeType.Html:
                    Span<Keyhole> html = buffer.AsSpan(keyhole.Sequence);
                    WriteHtml(buffer, html, includeSentinels);
                    if (includeSentinels)
                    {
                        _jsonWriter.WriteStringValueSegment("<!--/key:", false);
                        WriteKey(keyhole.Key);
                        _jsonWriter.WriteStringValueSegment("-->", false);
                    }
                    break;
                case KeyholeType.Attribute:
                    Span<Keyhole> attribute = buffer.AsSpan(keyhole.Sequence);
                    WriteAttributeSequence(attribute);
                    _jsonWriter.WriteStringValueSegment("", true);
                    break;
                case KeyholeType.EventListener:
                    _jsonWriter.WriteStringValueSegment("\"keyholes['", false);
                    WriteKey(keyhole.Key);
                    switch (keyhole.TrimModifier)
                    {
                        case "":
                            _jsonWriter.WriteStringValueSegment("'].dispatchEvent(event)\" key:", false);
                            break;
                        case null:
                        default:
                            _jsonWriter.WriteStringValueSegment("'].dispatchEvent(event,'", false);
                            _jsonWriter.WriteStringValueSegment(keyhole.TrimModifier ?? "*", false);
                            _jsonWriter.WriteStringValueSegment("')\" key:", false);
                            break;
                    }
                    WriteKey(keyhole.Key);
                    break;
                case KeyholeType.Iterator:
                    int start = keyhole.Sequence.Start.Value;
                    int end = keyhole.Sequence.End.Value;
                    for (int i2 = start; i2 < end; i2++)
                    {
                        ref var k = ref buffer[i2];
                        Span<Keyhole> iterator = buffer.AsSpan(k.Sequence);
                        WriteHtml(buffer, iterator, true);
                        if (includeSentinels)
                        {
                            _jsonWriter.WriteStringValueSegment("<!--/key:", false);
                            WriteKey(keyhole.Key);
                            _jsonWriter.WriteStringValueSegment("-->", false);
                        }
                    }
                    break;
                // The rest are the mutable keyhole values.  They might use format strings.
                default:
                    if (includeSentinels)
                    {
                            _jsonWriter.WriteStringValueSegment("<!--key:", false);
                            WriteKey(keyhole.Key);
                            _jsonWriter.WriteStringValueSegment("-->", false);
                    }

                    WriteMutableKeyholeValue(ref keyhole);

                    if (includeSentinels)
                    {
                        _jsonWriter.WriteStringValueSegment("<!--/key:", false);
                        WriteKey(keyhole.Key);
                        _jsonWriter.WriteStringValueSegment("-->", false);
                    }
                    break;
            }
        }
    }

    private void WriteAttributeSequence(Span<Keyhole> keyholes)
    {
        for (int i = 0; i < keyholes.Length; i++)
        {
            ref var keyhole = ref keyholes[i];

            switch (keyhole.Type)
            {
                case KeyholeType.StringLiteral:
                    _jsonWriter.WriteStringValueSegment(keyhole.StringLiteral, false);
                    break;
                // The rest are the mutable keyhole values.  They might use format strings.
                default:
                    WriteMutableKeyholeValue(ref keyhole);
                    break;
            }
        }
    }

    public void WriteRequest(int id)
    {
        // TODO: Implement
    }

    public void WriteResponse(int id)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");
        _jsonWriter.WriteNull("result");
        _jsonWriter.WriteNumber("id", id);
        _jsonWriter.WriteEndObject();
        _jsonWriter.Flush();

        OnMessageEnd();
    }

    public void WriteResponse<T>(int id, T result)
    {
        OnMessageBegin();

        _jsonWriter.WriteStartObject();
        _jsonWriter.WriteString("jsonrpc", "2.0");
        _jsonWriter.WritePropertyName("result");
        WriteTValue(result);
        _jsonWriter.WriteNumber("id", id);
        _jsonWriter.WriteEndObject();
        _jsonWriter.Flush();

        OnMessageEnd();
    }

    private void OnMessageBegin()
    {
        if (_isBatch && _jsonWriter.BytesCommitted + _jsonWriter.BytesPending + _bufferWriter.WrittenCount == 0)
            _jsonWriter.WriteStartArray();
    }

    private void OnMessageEnd()
    {
        if (!_isBatch)
            Flush();
    }

    private void WriteTValue<T>(T value)
    {
        switch (value)
        {
            case string s:
                _jsonWriter.WriteStringValue(s);
                break;
            case int i:
                _jsonWriter.WriteNumberValue(i);
                break;
            case bool b:
                _jsonWriter.WriteBooleanValue(b);
                break;
            // TODO: Support the rest.
            default:
                _jsonWriter.WriteNullValue();
                break;
        }
    }

    private void WriteMutableKeyholeValue(ref Keyhole keyhole)
    {
        // String and Boolean do not use format strings.
        switch (keyhole.Type)
        {
            case KeyholeType.String:
                // Must use jsonWriter to write this string with the proper json encoding.
                _jsonWriter.WriteStringValueSegment(keyhole.String, false);
                return;
            case KeyholeType.Boolean:
                _jsonWriter.WriteStringValueSegment(keyhole.Boolean ? "true" : "false", false);
                return;
        }

        // All other mutable values might make use of a format string.  
        // Flush the JSON writer and switch to the raw buffer writer.
        // Use IUtf8SpanFormattable.TryFormat() to write without allocating memory.

        _jsonWriter.Flush();
        int length = 0;
        int sizeHint = 30;
        switch (keyhole.Type)
        {
            case KeyholeType.Integer:
                while (!keyhole.Integer.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Long:
                while (!keyhole.Long.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Float:
                while (!keyhole.Float.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Double:
                while (!keyhole.Double.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Decimal:
                while (!keyhole.Decimal.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.DateTime:
                while (!keyhole.DateTime.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.DateOnly:
                while (!keyhole.DateOnly.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.TimeSpan:
                while (!keyhole.TimeSpan.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.TimeOnly:
                while (!keyhole.TimeOnly.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Color:
                while (!keyhole.Color.TryFormat(_bufferWriter.GetSpan(sizeHint), out length, keyhole.FormatModifier))
                    GrowSizeHint(ref sizeHint);
                break;
            case KeyholeType.Uri:
                // TODO: Fix memory allocation and support format string?
                _bufferWriter.Write(keyhole.Uri!.ToString());
                break;
        }
        _bufferWriter.Advance(length);
    }

    private static void GrowSizeHint(ref int sizeHint)
    {
        sizeHint *= 2;
        if (sizeHint > (2 << 20)) // 1MB
            throw new NotSupportedException("🛑 It seems a keyhole value with a format string needed a buffer > 1MB.  Probably misuse?  Needs investigation.");
    }

    public void Dispose()
    {
        _jsonWriter.Dispose();
    }
}