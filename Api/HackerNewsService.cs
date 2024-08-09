using System.Diagnostics;
using System.Collections.Concurrent;

namespace Api;

/// <summary>
/// The service which periodically fetches and caches data using given HackerNews client.
/// The only public method which spouse to be used directly is <c>GetBestStoriesAsJsonAsync</c>
/// but there are a bunch more as this class is used as a HostedServices by ASP.NET
/// and has to have a couple of public methods to stop it and dispose resources.
/// </summary>
public class HackerNewsService : BackgroundService, IDisposable  {
    // hardcoded values picked arbitrary
    // Could be exposed in appsettings.json but in reality
    // should not be changed without careful consideration.
    private static readonly TimeSpan FETCH_DELAY = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromSeconds(5);

    // It seems to work the fastest with just two clients.
    // Way faster than with just one and the extensive tests
    // with more clients provided unstable results.
    // With 100 clients I managed to download all the stories under 300 ms but
    // it takes a while to establish connections with 100 clients and the
    // performance gain from that was unstable.
    private readonly IHackerNewsClient[] _clients = new IHackerNewsClient[2];

    // To not to have to allocate the array every time
    private Task<HackerNewsStory>[] _tasks = Array.Empty<Task<HackerNewsStory>>();

    // Before first data is fetched we have to give something to await on.
    // After first data is fetched it's not relevant anymore.
    private TaskCompletionSource<bool> _fetchedFirstStories = new TaskCompletionSource<bool>();

    // The collection of previously fetched stories.
    // Can be safely used in different thread by just making a local copy of the reference
    // as the data within it is never modified or moved around and when doing next fetch
    // a new array is always allocated.
    private volatile HackerNewsStory[] _stories = Array.Empty<HackerNewsStory>();

    // The stories cache.  We assume that we can reuse stories for some time
    // specified in CACHE_EXPIRATION to not to have to fetch every story every time.
    private ConcurrentDictionary<int, CachedHackerNewsStory> _storiesCache
        = new ConcurrentDictionary<int, CachedHackerNewsStory>(-1, 1024);

    private readonly ILogger<HackerNewsService> _logger;
    private bool _disposed = false;

    /// <summary>
    /// Creates the service.
    /// Needs a clients factory 'cause it decides how many clients actually needs.
    /// </summary>
    public HackerNewsService(ILogger<HackerNewsService> logger, Func<IHackerNewsClient> clientFactory) {
        _logger = logger;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i] = clientFactory();
        }
    }


    /// <summary>
    /// Returns the specified amount of best stories from Hacker News
    /// as JSON Array sorted in descending order by their Score.
    /// There is a potential wait if the first data fetch is still in progress.
    /// After that all other calls are instantaneous hence the ValueTask type is used.
    /// </summary>
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

    /// <summary>
    /// This method is called by the ASP.NET at the start.
    /// It has the main loop which runs concurrently to the main thread
    /// and performs regular data fetches from the Hacker News API.
    /// The delay between fetches is specified by FETCH_DELAY.
    ///
    /// Is also logs how long it took to fetch the data - you have to enable
    /// Debug LogLevel in the appsettings.json tho.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken ct) {
        try {
            var stopWatch = new Stopwatch();
            _logger.LogInformation($"{nameof(HackerNewsService)}: main loop started");
            while (!ct.IsCancellationRequested) {
                stopWatch.Start();
                await FetchNewData(ct);
                stopWatch.Stop();
                _logger.LogDebug($"{nameof(HackerNewsService)}: {DateTime.UtcNow.ToString("o")} fetched {_stories.Length} stories in {stopWatch.Elapsed.TotalMilliseconds} ms");
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

    /// <summary>
    /// Stops the service background thread.
    /// Called by the ASP.NET
    /// </summary>
    public override async Task StopAsync(CancellationToken ct) {
        _logger.LogInformation($"{nameof(HackerNewsService)} stopped");
        await base.StopAsync(ct);
    }

    /// <summary>
    /// The actuall fetching of the data is here.
    /// This is called in the main loop of <c>ExecuteAsync</c> method and
    /// should not be executed in any other context.
    /// Will fetch the data from the HackerNews API or use/update the Cache.
    /// </summary>
    private async Task FetchNewData(CancellationToken ct) {
        try {
            // always get entire list of best stories
            var bestStoriesIds = await _clients[0].GetNBestStoriesIdsAsync(int.MaxValue, ct);
            // if our bucket of tasks is not enough allocate a new collection
            if (_tasks.Length < bestStoriesIds.Length)
                _tasks = new Task<HackerNewsStory>[bestStoriesIds.Length];
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

    // does what it says
    // just a helper method to have less code in the FetchNewData method
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

    // does what it says
    // just a helper method to have less code in the FetchNewData method
    private void AddStoryCache(HackerNewsStory story, DateTime fetchTime) {
        if (_storiesCache.ContainsKey(story.Id)) return;
        // intentionally ignoring the return value as it fails to add only if
        // the key already has assigned value which we know won't be the case
        _ = _storiesCache.TryAdd(story.Id, new CachedHackerNewsStory() {
            fetchTime = fetchTime,
            story = story,
        });
    }

    // Free the resources held by the clients.
    // For more details read the <c>HackerNewsClient</c> class comments.
    public override void Dispose() {
        if (_disposed) return;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i].Dispose();
        }
        _disposed = true;
        base.Dispose();
    }

    /// <summary>
    /// Pairs the chached story with the time it was fetched.
    /// </summary>
    private class CachedHackerNewsStory {
        public required DateTime fetchTime;
        public required HackerNewsStory story;
    }
}

