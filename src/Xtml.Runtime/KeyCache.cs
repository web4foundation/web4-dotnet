namespace Keyholes;

class KeyCache
{
    private readonly List<KeyCache?> _children = [];
    public static KeyCache Root { get; } = new KeyCache(null!, []);
    public byte[] Key { get; private set; }
    public KeyCache Parent { get; private set; }

    public byte[]? this[int index]
    {
        get => index < _children.Count ? _children[index]?.Key : null;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            while (_children.Count < index)
                _children.Add(null);

            if (index == _children.Count)
                _children.Add(new KeyCache(this, value));
            else
                _children[index] = new KeyCache(this, value);
        }
    }

    private KeyCache(KeyCache parent, byte[] key)
    {
        Parent = parent;
        Key = key;
    }

    public KeyCache NextGeneration(int index)
    {
        return _children[index] ??= new KeyCache(this, Parent.Key);
    }
}