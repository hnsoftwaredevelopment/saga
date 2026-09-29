using EbookManager.Presentation.Images;
using FluentAssertions;

namespace EbookManager.Tests.App.Images;

public sealed class CoverThumbnailCacheTests
{
    [Fact]
    public async Task GetAsync_reuses_a_cached_thumbnail()
    {
        var loads = 0;
        var cache = new CoverThumbnailCache<string>(
            capacity: 4,
            (path, width, _) =>
            {
                loads++;
                return Task.FromResult<string?>($"{path}:{width}");
            });

        var first = await cache.GetAsync("cover-a.jpg", 48, default);
        var second = await cache.GetAsync("cover-a.jpg", 48, default);

        first.Should().Be("cover-a.jpg:48");
        second.Should().Be(first);
        loads.Should().Be(1);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_evicts_the_least_recently_used_thumbnail_at_capacity()
    {
        var loads = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var cache = new CoverThumbnailCache<string>(
            capacity: 2,
            (path, _, _) =>
            {
                loads[path] = loads.GetValueOrDefault(path) + 1;
                return Task.FromResult<string?>(path);
            });

        await cache.GetAsync("cover-a.jpg", 48, default);
        await cache.GetAsync("cover-b.jpg", 48, default);
        await cache.GetAsync("cover-a.jpg", 48, default);
        await cache.GetAsync("cover-c.jpg", 48, default);
        await cache.GetAsync("cover-b.jpg", 48, default);

        loads["cover-a.jpg"].Should().Be(1);
        loads["cover-b.jpg"].Should().Be(2);
        loads["cover-c.jpg"].Should().Be(1);
        cache.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_caches_a_missing_thumbnail_without_repeated_io()
    {
        var loads = 0;
        var cache = new CoverThumbnailCache<string>(
            capacity: 4,
            (_, _, _) =>
            {
                loads++;
                return Task.FromResult<string?>(null);
            });

        (await cache.GetAsync("missing.jpg", 48, default)).Should().BeNull();
        (await cache.GetAsync("missing.jpg", 48, default)).Should().BeNull();

        loads.Should().Be(1);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_does_not_cache_a_cancelled_load()
    {
        var loads = 0;
        var firstLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new CoverThumbnailCache<string>(
            capacity: 4,
            async (path, _, cancellationToken) =>
            {
                loads++;
                if (loads == 1)
                {
                    firstLoadStarted.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return path;
            });
        using var cancellation = new CancellationTokenSource();
        var cancelledLoad = cache.GetAsync("cover.jpg", 48, cancellation.Token);
        await firstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await cancelledLoad.Awaiting(task => task).Should().ThrowAsync<OperationCanceledException>();
        (await cache.GetAsync("cover.jpg", 48, default)).Should().Be("cover.jpg");
        loads.Should().Be(2);
        cache.Count.Should().Be(1);
    }
}
