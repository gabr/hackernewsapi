namespace Api;

/// <summary>
/// Just so the HackerNewsService could work either with real or mocked data.
/// Also enforces the IDisposable interface - read <c>HackerNewsClien</c> class for details.
/// </summary>
public interface IHackerNewsClient : IDisposable {
    /// <summary>
    /// Gets all the best stories ids from the HackerNews.
    /// </summary>
    Task<int[]> GetAllBestStoriesIdsAsync(CancellationToken ct);

    /// <summary>
    /// Gets specified story details.
    /// The provided id has to come from the <c>GetBestStoriesIdsAsync</c>
    /// otherwise you might get unexpected results.
    /// </summary>
    Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct);
}
