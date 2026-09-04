using System.Collections.Concurrent;

namespace EbookManager.Application.Metadata;

internal static class BookCoverOperationLock
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Gates = new();

    public static async Task<IDisposable> AcquireAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var gate = Gates.GetOrAdd(bookId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new Releaser(gate);
    }

    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? gate = gate;

        public void Dispose() => Interlocked.Exchange(ref gate, null)?.Release();
    }
}
