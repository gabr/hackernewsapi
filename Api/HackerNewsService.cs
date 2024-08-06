using System.Diagnostics;

namespace Api;

public class HackerNewsService : BackgroundService, IDisposable  {
    /* NOTE: An arbitrary number of clients for faster queries
     *
     * Hacker News seems to limit how fast it can accept new connections from
     * given address.  The more connections we have the more requests can we
     * pipeline and having like 100 clients connected allows us to query 100
     * stories almost in an instant (200 ms).  But waiting for those 100
     * clients to connect for the first time takes almost a minute.
     *
     * Actually having just two connections seems to be optimal both or first
     * and subsequent queries allowing us to have all the responses in around
     * two to three seconds.  So start with two and gradually increase the
     * connected clients over time.
     */
    private readonly IHackerNewsClient[] _clients = new IHackerNewsClient[50];
    private int _activeClients = 2;
    private Task<HackerNewsStory>[] _tasks = new Task<HackerNewsStory>[HackerNewsClient.MAX_IDS_COUNT];
    private volatile HackerNewsStory[] _stories = Array.Empty<HackerNewsStory>();
    private readonly ILogger<HackerNewsService> _logger;
    private bool _disposed = false;

    public HackerNewsService(ILogger<HackerNewsService> logger, Func<IHackerNewsClient> clientFactory) {
        _logger = logger;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i] = clientFactory();
        }
    }

    public HackerNewsStory[] GetBestStories(int count) {
        Console.WriteLine($">>> GetBestStories({count}");
        if (count <= 0) return Array.Empty<HackerNewsStory>();
        var stories = _stories;
        Console.WriteLine($">>> stories.Length = {stories.Length}");
        return stories.Take(count).OrderByDescending(s => s.Score).ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken ct) {
        try {
            var stopWatch = new Stopwatch();
            _logger.LogInformation($"{nameof(HackerNewsService)}: main loop started");
            while (!ct.IsCancellationRequested) {
                stopWatch.Start();
                await FetchNewData(ct);
                stopWatch.Stop();
                _logger.LogInformation($"{nameof(HackerNewsService)}: {DateTime.UtcNow.ToString("o")} fetched data - elapsed: {stopWatch.Elapsed}");
                stopWatch.Reset();
                // double check before the dealy as the fetching might have taken a while
                if (ct.IsCancellationRequested) break;
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
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
            var bestStoriesIds = await _clients[0].GetNBestStoriesIdsAsync(HackerNewsClient.MAX_IDS_COUNT, ct);
            _logger.LogInformation($"{nameof(HackerNewsService)} using {_activeClients}/{_clients.Length} clients");
            var stories = new HackerNewsStory[bestStoriesIds.Length];
            // kick start all the requests
            Parallel.For(0, stories.Length, (i, _) => _tasks[i] = _clients[i%_activeClients].GetStoryByIdAsync(bestStoriesIds[i], ct));
            // use more connections next time
            if (_activeClients != _clients.Length) {
                _activeClients += 2;
                if (_activeClients > _clients.Length)
                    _activeClients = _clients.Length;
            }
            // wait for the responses and parse
            await Parallel.ForAsync(0, stories.Length, ct, async (i, _) => stories[i] = await _tasks[i]);
            _stories = stories;
        } catch (Exception ex) {
            _logger.LogError($"Fetching data exception: '{ex.ToString()}'", ex);
        }
    }

    public override void Dispose() {
        if (_disposed) return;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i].Dispose();
        }
        _disposed = true;
        base.Dispose();
    }
}

