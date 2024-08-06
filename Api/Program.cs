namespace Api;

public class Program
{
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        var mockData = builder.Configuration.GetValue<bool>("MockData");
        Func<IHackerNewsClient> clientFactory = mockData ?
            () => new HackerNewsClientMock(TimeSpan.FromMilliseconds(100)) :
            () => new HackerNewsClient();
        builder.Services.AddSingleton<Func<IHackerNewsClient>>(clientFactory);
        builder.Services.AddSingleton<HackerNewsService>();
        builder.Services.AddHostedService<HackerNewsService>(s => s.GetRequiredService<HackerNewsService>());
        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        if (mockData) logger.LogInformation("using mocked data");
        using var hackerNewsService = app.Services.GetRequiredService<HackerNewsService>();
        app.MapGet("/best", async (Int32 n = 10) => Results.Content(
            await hackerNewsService.GetBestStoriesAsJsonAsync(n),
            System.Net.Mime.MediaTypeNames.Application.Json));
        app.Run();
    }
}
