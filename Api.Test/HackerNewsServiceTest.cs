using Api;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Api.Test;

public class HackerNewsServiceTest {
    private JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
    private TimeSpan _timeout = TimeSpan.FromSeconds(10);
    private LoggerMock<HackerNewsService> _loggerMock = new LoggerMock<HackerNewsService>();
    private HackerNewsWebApiClientMock _clientMock = new HackerNewsWebApiClientMock();
    private HackerNewsService? _service;
    private Task _serviceTask = Task.CompletedTask;
    private CancellationTokenSource _serviceCancelationTokenSource = new CancellationTokenSource();

     [OneTimeSetUp]
     public void OneTimeSetUp() {
        _loggerMock.Enabled = true;
        _service = new HackerNewsService(_loggerMock, () => _clientMock);
        _serviceTask = Task.Factory.StartNew(
            () => _service.StartAsync(_serviceCancelationTokenSource.Token).Wait(),
            TaskCreationOptions.LongRunning);
     }

     [OneTimeTearDown]
     public void OneTimeTearDown() {
        if (_service != null) {
            _service.StopAsync(_serviceCancelationTokenSource.Token).Wait(_timeout);
            _clientMock.Dispose();
            _service.Dispose();
            _serviceCancelationTokenSource.Dispose();
        }
     }

     [TestCase(2)]
     [TestCase(10)]
     [TestCase(100)]
     [TestCase(200)]
     public void StoriesAreSorted(int count) {
        var stories = GetStoriesFromService(count);
        for (int i = 1; i < count; i++) {
            Assert.That(stories[i-1].Score, Is.GreaterThanOrEqualTo(stories[i].Score));
        }
     }

     [Test]
     public void EmptyCollectionForNegativeCount() {
        var stories = GetStoriesFromService(-1);
        Assert.That(stories.Length, Is.EqualTo(0));
     }

     [Test]
     public void WaitsForFirstStoriesFetch() {
        using var client = new HackerNewsWebApiClientMock();
        client.Mutex.WaitOne(); // do not allow the client to return any stories
        using var service = new HackerNewsService(new LoggerMock<HackerNewsService>(), () => client);
        using var cts = new CancellationTokenSource();
        // start the service
        Task.Factory.StartNew(() => service.StartAsync(cts.Token).Wait(), TaskCreationOptions.LongRunning);
        // and wait a while for it to try to fetch data
        Task.Delay(TimeSpan.FromSeconds(1));
        // now try to get data - which should not return
        var valueTask = service.GetBestStoriesAsJsonAsync(1);
        Assert.That(valueTask.IsCompleted, Is.False);
        // now wait a litte more - the task still should not be finished
        var task = valueTask.AsTask();
        var finished = task.Wait(TimeSpan.FromSeconds(1));
        Assert.That(finished, Is.False);
        Assert.That(valueTask.IsCompleted, Is.False);
        // now release the mutex and this should allow the service to get the data
        client.Mutex.ReleaseMutex();
        finished = task.Wait(_timeout);
        Assert.That(finished, Is.True);
        Assert.That(valueTask.IsCompleted, Is.True);
        var json = task.Result;
        Assert.That(json.Length, Is.GreaterThan(0));
        service.StopAsync(cts.Token).Wait(_timeout);
     }

     [Test]
     public void CachesData() {
        // just get some stories to make sure the service fetched the data
        var stories1 = GetStoriesFromService(1);
        Assert.That(stories1.Length, Is.GreaterThan(0));
        // now block the client
        _clientMock.Mutex.WaitOne();
        // we should still be able to get stories even tho the client is
        // blocked as the service should cache the data for some time
        var stories2 = GetStoriesFromService(1);
        Assert.That(stories2.Length, Is.GreaterThan(0));
        _clientMock.Mutex.ReleaseMutex();
     }

     private HackerNewsStory[] GetStoriesFromService(int count) {
        if (_service == null) return Array.Empty<HackerNewsStory>();
        var task = _service.GetBestStoriesAsJsonAsync(count).AsTask();
        task.Wait(_timeout);
        var json = task.Result;
        return JsonSerializer.Deserialize<HackerNewsStory[]>(json, _jsonSerializerOptions) ??
            Array.Empty<HackerNewsStory>();
     }
}
