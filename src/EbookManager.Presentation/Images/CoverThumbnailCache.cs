namespace EbookManager.Presentation.Images;

public sealed class CoverThumbnailCache<TThumbnail>
    where TThumbnail : class
{
    private const string MissingSourceVersion = "missing";
    private readonly int capacity;
    private readonly Func<string, int, CancellationToken, Task<TThumbnail?>> loader;
    private readonly Dictionary<string, CacheEntry> entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<string> recency = [];
    private readonly SemaphoreSlim loadGate;
    private readonly Lock sync = new();

    public CoverThumbnailCache(
        int capacity,
        Func<string, int, CancellationToken, Task<TThumbnail?>> loader,
        int maxConcurrentLoads = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentLoads);
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        this.capacity = capacity;
        loadGate = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);
    }

    public int Count
    {
        get
        {
            lock (sync)
            {
                return entries.Count;
            }
        }
    }

    public async Task<TThumbnail?> GetAsync(
        string path,
        int decodePixelWidth,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(decodePixelWidth);
        cancellationToken.ThrowIfCancellationRequested();

        var sourceVersion = await GetSourceVersionAsync(path, cancellationToken).ConfigureAwait(false);
        var key = CreateKey(path, decodePixelWidth, sourceVersion);
        lock (sync)
        {
            if (entries.TryGetValue(key, out var cached))
            {
                MarkMostRecent(cached.Node);
                return cached.Thumbnail;
            }
        }

        var thumbnail = await LoadAsync(path, decodePixelWidth, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (thumbnail is null && sourceVersion != MissingSourceVersion)
        {
            return null;
        }

        lock (sync)
        {
            if (entries.TryGetValue(key, out var cached))
            {
                MarkMostRecent(cached.Node);
                return cached.Thumbnail;
            }

            var node = recency.AddFirst(key);
            entries.Add(key, new CacheEntry(thumbnail, node));
            if (entries.Count > capacity && recency.Last is { } leastRecent)
            {
                entries.Remove(leastRecent.Value);
                recency.RemoveLast();
            }
        }

        return thumbnail;
    }

    private async Task<TThumbnail?> LoadAsync(
        string path,
        int decodePixelWidth,
        CancellationToken cancellationToken)
    {
        await loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await loader(path, decodePixelWidth, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            loadGate.Release();
        }
    }

    private static string CreateKey(string path, int decodePixelWidth, string sourceVersion) =>
        $"{decodePixelWidth}\0{sourceVersion}\0{path}";

    private static Task<string> GetSourceVersionAsync(
        string path,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var file = new FileInfo(path);
                return file.Exists
                    ? $"{file.LastWriteTimeUtc.Ticks}:{file.Length}"
                    : MissingSourceVersion;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or ArgumentException or
                    NotSupportedException)
            {
                return "unavailable";
            }
        }, cancellationToken);

    private void MarkMostRecent(LinkedListNode<string> node)
    {
        recency.Remove(node);
        recency.AddFirst(node);
    }

    private sealed record CacheEntry(TThumbnail? Thumbnail, LinkedListNode<string> Node);
}
