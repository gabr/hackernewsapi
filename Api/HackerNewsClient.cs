namespace Api;

public class HackerNewsClient : IHackerNewsClient {
    // not acutally mentioned in the docs - determined experimentally
    public const int MAX_IDS_COUNT = 200;

    private bool _disposed = false;
    private static readonly UriBuilder _bestStoriesUriBuilder = new UriBuilder("https://hacker-news.firebaseio.com/v0/beststories.json");

    private HttpClient _client = new HttpClient();

    public async Task<int[]> GetNBestStoriesIdsAsync(int n) {
        if (n <= 0) return Array.Empty<int>();
        if (n > MAX_IDS_COUNT) n = MAX_IDS_COUNT;
        _bestStoriesUriBuilder.Query = $"?orderBy=\"$key\"&limitToFirst={n}";
        var bestStoriesIdsResponse = await _client.GetAsync(_bestStoriesUriBuilder.Uri);
        bestStoriesIdsResponse.EnsureSuccessStatusCode();
        return await bestStoriesIdsResponse.Content.ReadFromJsonAsync<int[]>() ??
            throw new Exception($"Failed to deserialize HackerNews list of best stories ids");
    }

    public async Task<HackerNewsStory> GetStoryByIdAsync(int id) {
        var storyResponse = await _client.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");
        storyResponse.EnsureSuccessStatusCode();
        return await storyResponse.Content.ReadFromJsonAsync<HackerNewsStory>() ??
            throw new Exception($"Failed to deserialize HackerNews story id: {id}");
    }

    public void Dispose() {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
    }
}

