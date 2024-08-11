namespace Api;

/// <summary>
/// Returns static data instead of actually calling the HackerNews API.
/// Done to test performance and behavior of the code without the added
/// noise of the network and the HN API latencies.  The data returned by the
/// class is fixed and predictable the same with configurable delay.
/// </summary>
public class HackerNewsStaticDataClient : IHackerNewsClient {
    public const int STORIES_COUNT = 200;
    private TimeSpan _delay;

    /// <summary>
    /// Create class with added delaty to each call.
    /// </summary>
    public HackerNewsStaticDataClient(TimeSpan delay) {
        _delay = delay;
    }

    /// <summary>
    /// Gets the specified amount of stories ids.
    /// These are just the integers from 0 to n-1.
    /// </summary>
    public async Task<int[]> GetAllBestStoriesIdsAsync(CancellationToken ct) {
        await Task.Delay(_delay, ct);
        var ids = new int[STORIES_COUNT];
        for (int i = 0; i < ids.Length; i++) ids[i] = i;
        return ids;
    }

    /// <summary>
    /// Generate story with test data with given id.
    /// The only variable data here will be the Time field.  It is made so to
    /// have real value comparable to one which is returned by the actual API.
    /// </summary>
    public async Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct) {
        await Task.Delay(_delay, ct);
        return new HackerNewsStory {
            Title       = $"Test story with id: {id}",
            Time        = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Score       = id,
            By          = "static data client",
            Descendants = 0,
            Url         = "https://static/data/fake/url.html",
        };
    }

    // implementation not needed but interface forces us to put it here
    public void Dispose() { }
}

