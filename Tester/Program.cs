using System.Diagnostics;
using System.Text.Json;

using var client = new HttpClient();
var stopWatch = new Stopwatch();

async Task Get(string url) {
    Console.WriteLine($"GET: '{url}'");
    stopWatch.Reset();
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

await Parallel.ForAsync(0, 1000,
    new ParallelOptions { MaxDegreeOfParallelism = 100 },
    async (_, _) => {
        //await Get("http://localhost:5000/best?n=-10832423498");
        //await Get("http://localhost:5000/best?n=-01");
        //await Get("http://localhost:5000/best?n=1");
        //await Get("http://localhost:5000/best?n=101");
        await Get("http://localhost:5000/best?n=200");
        //await Get("http://localhost:5000/best?n=500");
        //await Get("http://localhost:5000/best?n=10730184832423498");
    });

