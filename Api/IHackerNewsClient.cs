namespace Api;

/// <summary>
/// Just so the HackerNewsService could work either with real or mocked data.
/// Also enforces the IDisposable interface - read <c>HackerNewsClien</c> class for details.
/// </summary>
public interface IHackerNewsClient : IDisposable {
    /// <summary>
    /// Gets the specified amount of best stories ids from the HackerNews.
    /// The <c>n</c> has to be in range from 1 to <c>MAX_IDS_COUNT</c> otherwise it will be clamped.
    /// </summary>
    Task<int[]> GetNBestStoriesIdsAsync(int n, CancellationToken ct);

    /// <summary>
    /// Gets specified story details.
    /// The provided id has to come from the <c>GetNBestStoriesIdsAsync</c>
    /// otherwise you might get unexpected results.
    /// </summary>
    Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct);
}
