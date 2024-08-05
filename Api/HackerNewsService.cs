namespace Api;

public class HackerNewsService : IDisposable {
    private bool _disposed = false;
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
    private DateTime _activeClientsIncreaseTime = DateTime.MinValue;
    private Task<HackerNewsStory>[] _tasks = new Task<HackerNewsStory>[HackerNewsClient.MAX_IDS_COUNT];

    public HackerNewsService(Func<IHackerNewsClient> clientFactory) {
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i] = clientFactory();
        }
    }

    public async Task<HackerNewsStory[]> GetBestStoriesAsync(int count) {
        var bestStoriesIds = await _clients[0].GetNBestStoriesIdsAsync(count);
        if (bestStoriesIds.Length == 0) return Array.Empty<HackerNewsStory>();
        var stories = new HackerNewsStory[bestStoriesIds.Length];
        // kick start all the requests
        Parallel.For(0, stories.Length, (i, _) => _tasks[i] = _clients[i%_activeClients].GetStoryByIdAsync(bestStoriesIds[i]));
        // use more connections next time
        if (_activeClientsIncreaseTime != DateTime.MinValue) {
            var diff = DateTime.UtcNow - _activeClientsIncreaseTime;
            if (diff.TotalSeconds > 1.5) {
                _activeClients += 2;
                _activeClientsIncreaseTime = DateTime.UtcNow;
            }
        }
        // wait for the responses and parse
        await Parallel.ForAsync(0, stories.Length, async (i, _) => stories[i] = await _tasks[i]);
        return stories.OrderByDescending(s => s.Score).ToArray();
    }

    public void Dispose() {
        if (_disposed) return;
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i].Dispose();
        }
        _disposed = true;
    }
}

