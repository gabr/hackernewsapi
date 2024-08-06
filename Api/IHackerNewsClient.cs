namespace Api;

public interface IHackerNewsClient : IDisposable {
    Task<int[]> GetNBestStoriesIdsAsync(int n, CancellationToken ct);
    Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct);
}
