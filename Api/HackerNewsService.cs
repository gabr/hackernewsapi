using System.Diagnostics;
using System.Collections.Concurrent;

namespace Api;

public class HackerNewsService : BackgroundService, IDisposable  {
    private static readonly TimeSpan FETCH_DELAY = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromSeconds(5);

    /* NOTE: It seems to work the fastest with just two clients>
     *  Way faster than with just one and the extensive tests
     *  with more clients provided unstable results.
     */
    private readonly IHackerNewsClient[] _clients = new IHackerNewsClient[2];
    private Task<HackerNewsStory>[] _tasks = new Task<HackerNewsStory>[HackerNewsClient.MAX_IDS_COUNT];
    private TaskCompletionSource<bool> _fetchedFirstStories = new TaskCompletionSource<bool>();
    private volatile HackerNewsStory[] _stories = Array.Empty<HackerNewsStory>();
    private ConcurrentDictionary<int, CachedHackerNewsStory> _storiesCache
        = new ConcurrentDictionary<int, CachedHackerNewsStory>(-1, HackerNewsClient.MAX_IDS_COUNT*2);
    private readonly ILogger<HackerNewsService> _logger;
    private bool _disposed = false;

    public HackerNewsService(ILogger<HackerNewsService> logger, Func<IHackerNewsClient> clientFactory) {
        _logger = logger;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i] = clientFactory();
        }
    }

    public async ValueTask<string> GetBestStoriesAsJsonAsync(int count) {
        if (count <= 0) return string.Empty;
        if (_stories.Length == 0) await _fetchedFirstStories.Task;
        var stories = _stories;
        return "[" +
            string.Join(",",
                stories
                    .Take(count)
                    .OrderByDescending(s => s.Score)
                    .Select(s => s.GetJson())
                    .ToArray())
            + "]";
    }

    protected override async Task ExecuteAsync(CancellationToken ct) {
        try {
            var stopWatch = new Stopwatch();
            _logger.LogInformation($"{nameof(HackerNewsService)}: main loop started");
            while (!ct.IsCancellationRequested) {
                stopWatch.Start();
                await FetchNewData(ct);
                stopWatch.Stop();
                _logger.LogDebug($"{nameof(HackerNewsService)}: {DateTime.UtcNow.ToString("o")} fetched data - elapsed: {stopWatch.Elapsed}");
                stopWatch.Reset();
                // double check before the dealy as the fetching might have taken a while
                if (ct.IsCancellationRequested) break;
                await Task.Delay(FETCH_DELAY, ct);
            }
            _logger.LogInformation($"{nameof(HackerNewsService)} main loop stopped");
        } catch (Exception ex) {
            _logger.LogError($"{nameof(HackerNewsService)}: Exception in main loop: '{ex.ToString()}'", ex);
        }
    }

    public override async Task StopAsync(CancellationToken ct) {
        _logger.LogInformation($"{nameof(HackerNewsService)} stopped");
        await base.StopAsync(ct);
    }

    private async Task FetchNewData(CancellationToken ct) {
        try {
            // always get entire list of best stories
            var bestStoriesIds = await _clients[0].GetNBestStoriesIdsAsync(HackerNewsClient.MAX_IDS_COUNT, ct);
            var stories = new HackerNewsStory[bestStoriesIds.Length];
            // kick start all the requests
            var fetchTime = DateTime.UtcNow;
            Parallel.For(0, stories.Length, (i, _) => {
                var id = bestStoriesIds[i];
                _tasks[i] = GetStoryFromCache(id, fetchTime) ??
                    // if not in cache then fetch it from the API
                    _clients[i%_clients.Length].GetStoryByIdAsync(id, ct);
            });
            // wait for the responses and update cache
            await Parallel.ForAsync(0, stories.Length, ct, async (i, _) => {
                stories[i] = await _tasks[i];
                AddStoryCache(stories[i], fetchTime);
            });
            // replace old collection with new data
            _stories = stories;
            // and inform waiting clients that data is available
            if (!_fetchedFirstStories.Task.IsCompleted)
                _fetchedFirstStories.SetResult(true);
        } catch (Exception ex) {
            _logger.LogError($"Fetching data exception: '{ex.ToString()}'", ex);
        }
    }

    private Task<HackerNewsStory>? GetStoryFromCache(int id, DateTime fetchTime) {
        if (!_storiesCache.ContainsKey(id)) return null;
        var cached = _storiesCache[id];
        if ((fetchTime - cached.fetchTime) > CACHE_EXPIRATION) {
            // intentionally ignoring return value as it should be false only
            // if value was not found which we know for sure is there
            _ = _storiesCache.TryRemove(id, out _); // remove if expired
            return null;
        }
        return Task.FromResult(cached.story);
    }

    private void AddStoryCache(HackerNewsStory story, DateTime fetchTime) {
        if (_storiesCache.ContainsKey(story.Id)) return;
        // intentionally ignoring the return value as it fails to add only if
        // the key already has assigned value which we know won't be the case
        _ = _storiesCache.TryAdd(story.Id, new CachedHackerNewsStory() {
            fetchTime = fetchTime,
            story = story,
        });
    }

    public override void Dispose() {
        if (_disposed) return;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i].Dispose();
        }
        _disposed = true;
        base.Dispose();
    }

    private class CachedHackerNewsStory {
        public required DateTime fetchTime;
        public required HackerNewsStory story;
    }
}

