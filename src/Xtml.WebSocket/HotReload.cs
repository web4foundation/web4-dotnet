#if !DEBUG

namespace Xtml.WebSocket;

public static class HotReload
{
    public static int ReloadCount { get; private set; } = 0;
}

#else

using System.Diagnostics;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Xtml.WebSocket.HotReload))]

namespace Xtml.WebSocket;

public static class HotReload
{
    public static int ReloadCount { get; private set; } = 0;
    public static event Action<Type[]?>? UpdateApplicationEvent;

    // Executes BEFORE the code changes take effect
    public static void ClearCache(Type[]? updatedTypes)
    {
    }

    // Executes AFTER the code changes take effect
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        ReloadCount++;
        UpdateApplicationEvent?.Invoke(updatedTypes);
    }
}
#endif