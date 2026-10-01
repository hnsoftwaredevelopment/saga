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
    public async Task GetAsync_retries_an_existing_thumbnail_after_a_temporary_load_failure()
    {
        var path = Path.GetTempFileName();
        try
        {
            var loads = 0;
            var cache = new CoverThumbnailCache<string>(
                capacity: 4,
                (_, _, _) =>
                {
                    loads++;
                    return Task.FromResult<string?>(loads == 1 ? null : "cover");
                });

            (await cache.GetAsync(path, 48, default)).Should().BeNull();
            (await cache.GetAsync(path, 48, default)).Should().Be("cover");

            loads.Should().Be(2);
            cache.Count.Should().Be(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetAsync_limits_parallel_thumbnail_loads()
    {
        var paths = Enumerable.Range(0, 8).Select(_ => Path.GetTempFileName()).ToArray();
        try
        {
            var activeLoads = 0;
            var maximumActiveLoads = 0;
            var twoLoadsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseLoads = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cache = new CoverThumbnailCache<string>(
                capacity: 16,
                async (path, _, cancellationToken) =>
                {
                    var active = Interlocked.Increment(ref activeLoads);
                    InterlockedExtensions.Max(ref maximumActiveLoads, active);
                    if (active == 2)
                    {
                        twoLoadsStarted.TrySetResult();
                    }

                    try
                    {
                        await releaseLoads.Task.WaitAsync(cancellationToken);
                        return path;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref activeLoads);
                    }
                },
                maxConcurrentLoads: 2);

            var loads = paths.Select(path => cache.GetAsync(path, 48, default)).ToArray();
            await twoLoadsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            maximumActiveLoads.Should().Be(2);
            releaseLoads.TrySetResult();
            await Task.WhenAll(loads);
        }
        finally
        {
            foreach (var path in paths)
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task GetAsync_removes_a_cancelled_thumbnail_from_the_load_queue()
    {
        var firstPath = Path.GetTempFileName();
        var cancelledPath = Path.GetTempFileName();
        try
        {
            var loads = 0;
            var firstLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cache = new CoverThumbnailCache<string>(
                capacity: 4,
                async (path, _, cancellationToken) =>
                {
                    Interlocked.Increment(ref loads);
                    firstLoadStarted.TrySetResult();
                    await releaseFirstLoad.Task.WaitAsync(cancellationToken);
                    return path;
                },
                maxConcurrentLoads: 1);

            var firstLoad = cache.GetAsync(firstPath, 48, default);
            await firstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var cancellation = new CancellationTokenSource();
            var cancelledLoad = cache.GetAsync(cancelledPath, 48, cancellation.Token);
            cancellation.Cancel();

            await cancelledLoad.Awaiting(task => task).Should().ThrowAsync<OperationCanceledException>();
            loads.Should().Be(1);

            releaseFirstLoad.TrySetResult();
            (await firstLoad).Should().Be(firstPath);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(cancelledPath);
        }
    }

    [Fact]
    public async Task GetAsync_reloads_a_thumbnail_when_the_source_file_changes()
    {
        var path = Path.GetTempFileName();
        try
        {
            var loads = 0;
            var cache = new CoverThumbnailCache<string>(
                capacity: 4,
                (sourcePath, _, _) =>
                {
                    loads++;
                    return Task.FromResult<string?>(File.ReadAllText(sourcePath));
                });
            File.WriteAllText(path, "first");
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            (await cache.GetAsync(path, 48, default)).Should().Be("first");

            File.WriteAllText(path, "second");
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

            (await cache.GetAsync(path, 48, default)).Should().Be("second");
            loads.Should().Be(2);
        }
        finally
        {
            File.Delete(path);
        }
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

file static class InterlockedExtensions
{
    public static void Max(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }
}
