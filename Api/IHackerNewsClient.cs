using Api;

public interface IHackerNewsClient : IDisposable {
    Task<int[]> GetNBestStoriesIdsAsync(int n);
    Task<HackerNewsStory> GetStoryByIdAsync(int id);
}
