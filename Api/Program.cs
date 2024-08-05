namespace Api;

public class Program
{
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        Func<IHackerNewsClient> hackerNewsClientFactory = app.Environment.IsDevelopment() ?
            () => new HackerNewsClientMock(TimeSpan.FromMilliseconds(500)) :
            () => new HackerNewsClient();
        using var hackerNewsService = new HackerNewsService(hackerNewsClientFactory);
        app.MapGet("/best", async (Int32 n = 10) => await hackerNewsService.GetBestStoriesAsync(n));
        app.Run();
    }
}
