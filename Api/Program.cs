namespace Api;

public class Program
{
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var mockData = app.Configuration.GetValue<bool>("MockData");
        if (mockData) logger.LogInformation("using mocked data");
        Func<IHackerNewsClient> hackerNewsClientFactory = mockData ?
            () => new HackerNewsClientMock(TimeSpan.FromMilliseconds(100)) :
            () => new HackerNewsClient();
        using var hackerNewsService = new HackerNewsService(hackerNewsClientFactory);
        app.MapGet("/best", async (Int32 n = 10) => await hackerNewsService.GetBestStoriesAsync(n));
        app.Run();
    }
}
