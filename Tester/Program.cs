using System.Diagnostics;
using System.Text.Json;

using var client = new HttpClient();
var stopWatch = new Stopwatch();

async Task<double> Get(string url) {
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
    return stopWatch.Elapsed.TotalMilliseconds;
}

async Task StressTest() {
    var elapsedMax = new double[1000];
    var elapsedMin = new double[1000];
    var elapsedAvr = new double[1000];
    await Parallel.ForAsync(0, 1000,
        new ParallelOptions { MaxDegreeOfParallelism = 100 },
        async (i, _) => {
            var e = new double[4];
            e[0] = await Get("http://localhost:5000/best?n=1");
            e[1] = await Get("http://localhost:5000/best?n=101");
            e[2] = await Get("http://localhost:5000/best?n=200");
            e[3] = await Get("http://localhost:5000/best?n=500");
            elapsedMax[i] = e.Max();
            elapsedMin[i] = e.Min();
            elapsedAvr[i] = e.Average();
        });
    var max = elapsedMax.Max();
    var min = elapsedMin.Min();
    var avr = elapsedAvr.Average();
    Console.WriteLine($"total - min: {min} ms, max: {max} ms, avr: {avr} ms");
}

//await StressTest();
await Get("http://localhost:5000/best?n=10");

