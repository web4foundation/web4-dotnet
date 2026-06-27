using System.Runtime.CompilerServices;

namespace Keyholes;

public class KeyCursor
{
    private const byte _separator = (byte)':';
    private readonly List<int> _keyDigits = [-1];
    private KeyCache _keyCache = KeyCache.Root;

    public int CurrentIndex => _keyDigits[CurrentDepth];
    public int CurrentDepth { get; private set; }
    public byte[] Parent => _keyCache.Key;

    public KeyCursor() => Reset();

    public void Reset()
    {
        CurrentDepth = 0;
        _keyDigits[0] = -1;
        _keyCache = KeyCache.Root;
    }

    public byte[] MoveNext()
    {
        _keyDigits[CurrentDepth]++;
        return GetOrCreateCurrent();
    }

    public void MoveDown()
    {
        _keyCache = _keyCache.NextGeneration(_keyDigits[CurrentDepth]);
        CurrentDepth++;

        if (_keyDigits.Count > CurrentDepth)
            _keyDigits[CurrentDepth] = -1;
        else
            _keyDigits.Add(-1);
    }

    public byte[] MoveUp()
    {
        _keyCache = _keyCache.Parent;
        CurrentDepth--;
        return GetOrCreateCurrent();
    }

    private byte[] GetOrCreateCurrent()
    {
        int index = _keyDigits[CurrentDepth];

        var key = _keyCache[index];
        if (key is not null)
            return key;

        key = GenerateKey(
            parentKey: _keyCache.Key, 
            childIndex: index
        );

        _keyCache[index] = key;

        return key;
    }

    private static byte[] GenerateKey(byte[] parentKey, int childIndex)
    {
        int length = GetLength(childIndex);
        byte[] buffer;
        Span<byte> dest;
        if (parentKey.Length == 0)
        {
            buffer = new byte[length];
            dest = buffer.AsSpan(0);
        }
        else
        {
            buffer = new byte[parentKey.Length + length + 1];
            parentKey.CopyTo(buffer);
            buffer[parentKey.Length] = _separator;
            dest = buffer.AsSpan(parentKey.Length + 1);
        }

        if (length >= 7) dest[^7] = ToHexChar(childIndex >> 24);
        if (length >= 6) dest[^6] = ToHexChar(childIndex >> 20);
        if (length >= 5) dest[^5] = ToHexChar(childIndex >> 16);
        if (length >= 4) dest[^4] = ToHexChar(childIndex >> 12);
        if (length >= 3) dest[^3] = ToHexChar(childIndex >> 8);
        if (length >= 2) dest[^2] = ToHexChar(childIndex >> 4);
        if (length >= 1) dest[^1] = ToHexChar(childIndex);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToHexChar(int value)
    {
        value &= 0b1111;
        return value switch
        {
            < 10 => (byte)(value + '0'),
            _ => (byte)(value - 10 + 'a')
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetLength(int number) => number switch
    {
        < 0x10 << 0 => 1,  // 0-f
        < 0x10 << 4 => 2,  // 10-ff
        < 0x10 << 8 => 3,  // 100-fff
        < 0x10 << 12 => 4, // 1000-ffff
        < 0x10 << 16 => 5, // 10000-fffff
        < 0x10 << 20 => 6, // 100000-fffff
        < 0x10 << 24 => 7, // 1000000-fffffff
        _ => throw new Exception($"Too large for an int: {number}")
    };
}