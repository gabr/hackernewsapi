using Api;

namespace Api.Test;

public class HackerNewsWebApiClientMock : IHackerNewsClient {
    /// <summary>
    /// Allows to block the methods so that the client does not return any
    /// values when service asks for them.
    /// </summary>
    public Mutex Mutex = new Mutex();

    public async Task<int[]> GetAllBestStoriesIdsAsync(CancellationToken ct) {
        await Task.CompletedTask;
        var ids = new int[HackerNewsStaticDataClient.STORIES_COUNT];
        for (int i = 0; i < ids.Length; i++) ids[i] = i;
        return ids;
    }

    public async Task<HackerNewsStory> GetStoryByIdAsync(int id, CancellationToken ct) {
        try {
            Mutex.WaitOne();
            await Task.CompletedTask;
            return new HackerNewsStory {
                Title       = $"Mock test story with id: {id}",
                Time        = DateTimeOffset.UnixEpoch.ToUnixTimeSeconds(),
                Score       = id,
                By          = "mock client",
                Descendants = 0,
                Url         = "mock url",
            };
        } finally {
            Mutex.ReleaseMutex();
        }
    }

    // implementation not needed but interface forces us to put it here
    public void Dispose() { }
}
