using System.Diagnostics;

namespace Keyholes.Utilities;

public static class Perf
{
    public static IDisposable Measure(string name = "unnamed") => new Disposer(name);

    private class Disposer(string name) : IDisposable
    {
        readonly long gc1 = GC.GetAllocatedBytesForCurrentThread();
        readonly long sw1 = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(sw1);
            long gc2 = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"{$"🚥 Perf({name}):",-45} elapsed:{$"{elapsed.TotalNanoseconds:n0} ns",-15} {$"allocations: {(gc2 - gc1):n0} bytes",-25}   thread:{Thread.CurrentThread.ManagedThreadId}");
        }
    }
}