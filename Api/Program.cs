namespace Api;

public class Program
{
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        // the "MockData" in appsettings.json allows to specify which client will be used for fetching the data.
        var mockData = builder.Configuration.GetValue<bool>("MockData");
        Func<IHackerNewsClient> clientFactory = mockData ?
            () => new HackerNewsClientMock(TimeSpan.FromMilliseconds(100)) :
            () => new HackerNewsClient();
        builder.Services.AddSingleton<Func<IHackerNewsClient>>(clientFactory);
        // The HackerNewsService is used mostly as a BackgroundService and periodically runs fetching code
        // but because we also need to access the fetched data in our API endpoint we register it as a regular
        // singleton service first and only after that we add it to HostedServices.
        // That way we have single and the same instance in both places.
        builder.Services.AddSingleton<HackerNewsService>();
        builder.Services.AddHostedService<HackerNewsService>(s => s.GetRequiredService<HackerNewsService>());

        var app = builder.Build();
        // grab the logger just to log out if the mocked data is used
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        if (mockData) logger.LogInformation("using mocked data");
        using var hackerNewsService = app.Services.GetRequiredService<HackerNewsService>();
        // I'm using the minimalistic API syntax as that's all I need for this application
        app.MapGet("/best", async (Int32 n = 10) => Results.Content(
            // we are getting the result already as a JSON - for performance
            // reasons (see the HackerNewsStory class) so we need to specify
            // the content type manually as it would be plain/text otherwise
            await hackerNewsService.GetBestStoriesAsJsonAsync(n),
            System.Net.Mime.MediaTypeNames.Application.Json));
        app.Run();
    }
}
