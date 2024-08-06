using System.Diagnostics;
using System.Text.Json;

using var client = new HttpClient();
var stopWatch = new Stopwatch();

async Task Get(string url) {
    Console.WriteLine($"GET: '{url}'");
    stopWatch.Start();
    var jsonString = await client.GetStringAsync(url);
    stopWatch.Stop();
    var json = JsonSerializer.Serialize(
        JsonDocument.Parse(jsonString),
        new JsonSerializerOptions() { WriteIndented = true });
    Console.WriteLine(json);
    Console.WriteLine($"elapsed: {stopWatch.Elapsed}");
}

//await Get("http://localhost:5000/best?n=101");

await Parallel.ForAsync(0, 10000, async (_, _) => await Get("http://localhost:5000/best?n=201"));

