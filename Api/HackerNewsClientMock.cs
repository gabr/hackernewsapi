namespace Api;

public class HackerNewsClientMock : IHackerNewsClient {
    private TimeSpan _delay;
    private int[] _ids = new int[HackerNewsClient.MAX_IDS_COUNT];

    public HackerNewsClientMock(TimeSpan delay) {
        _delay = delay;
        for (int i = 0; i < _ids.Length; i++) _ids[i] = i;
    }

    public async Task<int[]> GetNBestStoriesIdsAsync(int n) {
        await Task.Delay(_delay);
        if (n <= 0) return Array.Empty<int>();
        if (n > HackerNewsClient.MAX_IDS_COUNT) n = HackerNewsClient.MAX_IDS_COUNT;
        return _ids.Take(n).ToArray();
    }

    public async Task<HackerNewsStory> GetStoryByIdAsync(int id) {
        await Task.Delay(_delay);
        return new HackerNewsStory {
            Id          = id,
            Title       = $"Test story with id: {id}",
            Time        = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Score       = id,
            By          = "mock client",
            Descendants = id,
            Url         = "mock url",
        };
    }

    public void Dispose() { }
}

