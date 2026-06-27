#if !DEBUG

namespace Web4.WebSocket;

public static class HotReload
{
    public static int ReloadCount { get; private set; } = 0;
    public static IDisposable Listen(Func<Task> action) => new NoOp();
    class NoOp : IDisposable
    {
        public void Dispose() { }
    }
}

#else

using System.Diagnostics;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Web4.WebSocket.HotReload))]

namespace Web4.WebSocket;

public static class HotReload
{
    public static int ReloadCount { get; private set; } = 0;
    public static IDisposable Listen(Func<Task> action) => new HotReloadContext(action);

    #pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public static event Action<Type[]?>? UpdateApplicationEvent;
    #pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    internal static void ClearCache(Type[]? types)
    {
        types?.ToList().ForEach(type => Debug.WriteLine($"Hot Reload (ClearCache): {type.FullName}"));
    }

    internal static void UpdateApplication(Type[]? types)
    {
        ReloadCount++;

        types?.ToList().ForEach(type => Debug.WriteLine($"Hot Reload (UpdateApplication): {type.FullName}"));

        UpdateApplicationEvent?.Invoke(types);
    }
}

internal class HotReloadContext : IDisposable
{
    private readonly Func<Task> action;
    public HotReloadContext(Func<Task> action)
    {
        this.action = action;
        HotReload.UpdateApplicationEvent += OnHotReload;
    }

    public void OnHotReload(Type[]? obj)
    {
        Task.Run(async () =>
        {
            await action();
        });
    }

    public void Dispose()
    {
        HotReload.UpdateApplicationEvent -= OnHotReload;
    }
}
#endif