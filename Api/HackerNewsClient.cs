namespace Api;

/// <summary>
/// The Web API client used to fetch data from the HackerNews API.
/// </summary>
public class HackerNewsClient : IHackerNewsClient {
    // Not acutally mentioned in the docs - determined experimentally.
    // No matter what I did the maximum amount of the ids coming out
    // of the /beststories.json endpoint was 200.  Which is nice as that
    // allows to make some assumptions.
    // This field is made public so that we can have it in just one place
    // instead of duplicating it in other classes which take advantage of
    // this assumption.
    public const int MAX_IDS_COUNT = 200;

    private bool _disposed = false;
    private static readonly UriBuilder _bestStoriesUriBuilder = new UriBuilder("https://hacker-news.firebaseio.com/v0/beststories.json");

    // We don't want to create and dispose HttpClient every time we make a
    // request but that forced me to implement IDisposable interface
    // everywhere to free up system ports at the end.
    private HttpClient _client = new HttpClient();

    /// <summary>
    /// Gets the specified amount of best stories ids from the HackerNews API.
    /// The <c>n</c> has to be in range from 1 to <c>MAX_IDS_COUNT</c> otherwise it will be clamped.
    /// </summary>
    public async Task<int[]> GetNBestStoriesIdsAsync(int n, CancellationToken ct) {
        if (n <= 0) return Array.Empty<int>();
        if (n > MAX_IDS_COUNT) n = MAX_IDS_COUNT;
        _bestStoriesUriBuilder.Query = $"?orderBy=\"$key\"&limitToFirst={n}";
        using var bestStoriesIdsResponse = await _client.GetAsync(_bestStoriesUriBuilder.Uri, ct);
        bestStoriesIdsResponse.EnsureSuccessStatusCode();
        return await bestStoriesIdsResponse.Content.ReadFromJsonAsync<int[]>(ct) ??
            throw new Exception($"Failed to deserialize HackerNews list of best stories ids");
    }

    /// <summary>
    /// Gets specified story details.
    /// The provided id has to come from the <c>GetNBestStoriesIdsAsync</c>
    /// otherwise you might get unexpected results.
    /// </summary>
    public async Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct) {
        using var storyResponse = await _client.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json", ct);
        storyResponse.EnsureSuccessStatusCode();
        return await storyResponse.Content.ReadFromJsonAsync<HackerNewsStory>(ct) ??
            throw new Exception($"Failed to deserialize HackerNews story id: {id}");
    }

    // Necessary because we are holding on a HttpClient instance and
    // we want to to free up the resources (system ports) at the end.
    // This is intentionally not the full recommended implementation
    // of the IDisposable interface because after examination of the
    // HttpClient source code we can see that no work is done for
    // Dispose(false) call.  Therefore I simplified the code to keep
    // unnecessary boilerplate to minimum for this task.
    public void Dispose() {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
    }
}

