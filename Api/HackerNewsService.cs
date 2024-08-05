namespace Api;

public class HackerNewsService : IDisposable {
    private bool _disposed = false;
    static readonly IHackerNewsClient[] _clients = new IHackerNewsClient[HackerNewsClient.MAX_IDS_COUNT];

    public HackerNewsService(Func<IHackerNewsClient> clientFactory) {
        for (int i = 0; i < _clients.Length; i++) {
            _clients[i] = clientFactory();
        }
    }

    public async Task<HackerNewsStory[]> GetBestStoriesAsync(int count) {
        var bestStoriesIds = await _clients[0].GetNBestStoriesIdsAsync(count);
        if (bestStoriesIds.Length == 0) return Array.Empty<HackerNewsStory>();
        var stories = new HackerNewsStory[bestStoriesIds.Length];
        await Parallel.ForAsync(0, bestStoriesIds.Length,
            async (i, _) => {
                stories[i] = await _clients[i].GetStoryByIdAsync(bestStoriesIds[i]);
        });
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

