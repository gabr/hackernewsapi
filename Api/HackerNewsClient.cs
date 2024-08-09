namespace Api;

/// <summary>
/// The Web API client used to fetch data from the HackerNews API.
/// </summary>
public class HackerNewsClient : IHackerNewsClient {
    private bool _disposed = false;
    private static readonly UriBuilder _bestStoriesUriBuilder = new UriBuilder("https://hacker-news.firebaseio.com/v0/beststories.json");

    // We don't want to create and dispose HttpClient every time we make a
    // request but that forced me to implement IDisposable interface
    // everywhere to free up system ports at the end.
    private HttpClient _client = new HttpClient();

    /// <summary>
    /// Gets the specified amount of best stories ids from the HackerNews API.
    /// Negative values of <c>n</c> will result in empty result.
    /// </summary>
    public async Task<int[]> GetNBestStoriesIdsAsync(int n, CancellationToken ct) {
        if (n <= 0) return Array.Empty<int>();
        _bestStoriesUriBuilder.Query = n < int.MaxValue ?
            $"?orderBy=\"$key\"&limitToFirst={n}" :
            string.Empty;
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

