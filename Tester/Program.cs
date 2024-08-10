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
    Console.WriteLine($"elapsed: {stopWatch.Elapsed.TotalMilliseconds} ms");
}

async Task StressTest() {
    const int tasks = 10;
    const int requests = 10000;
    var elapsed = new double[requests];
    var stopwatches = Enumerable.Range(0, requests).Select(_ => new Stopwatch()).ToArray();
    var clients = Enumerable.Range(0, tasks).Select(_ => new HttpClient()).ToArray();
    var totalstopwatch = new Stopwatch();
    Console.WriteLine($"{DateTime.Now.ToString("o")} StressTest start");
    totalstopwatch.Start();
    await Parallel.ForAsync(0, requests,
        new ParallelOptions { MaxDegreeOfParallelism = tasks },
        async (i, _) => {
            stopwatches[i].Start();
            await clients[i%clients.Length].GetStringAsync("http://localhost:5000/best?n=500");
            stopwatches[i].Stop();
            elapsed[i] = stopwatches[i].Elapsed.TotalMilliseconds;
        });
    totalstopwatch.Stop();
    Console.WriteLine($"{DateTime.Now.ToString("o")} StressTest end");
    foreach (var c in clients) c.Dispose();
    var max = elapsed.Max();
    var min = elapsed.Min();
    var avr = elapsed.Average();
    var std = Math.Sqrt(elapsed.Select(e => Math.Pow(e-avr,2)).Average());
    Console.WriteLine($" requests per second: {requests/totalstopwatch.Elapsed.TotalSeconds}");
    Console.WriteLine($" total time: {totalstopwatch.Elapsed}");
    Console.WriteLine($" requests: {requests}");
    Console.WriteLine($" min: {min} ms");
    Console.WriteLine($" max: {max} ms");
    Console.WriteLine($" avr: {avr} ms");
    Console.WriteLine($" std: {std}");
}

await Get("http://localhost:5000/best?n=10");
await StressTest();

