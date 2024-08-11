using Api;
using System.Text.Json;

namespace Api.Test;

public class HackerNewsStoryTest {
    [Test]
    public void SerializeTest() {
        var story = new HackerNewsStory() {
            Title       = "Test1",
            Time        = DateTimeOffset.UnixEpoch.ToUnixTimeSeconds(),
            Score       = 7,
            By          = "Tester",
            Descendants = 255,
            Url         = "https://test/url.html",
        };
        var json = story.ToJson();
        Assert.That(json, Is.EqualTo(@"{""Title"":""Test1"",""Uri"":""https://test/url.html"",""PostedBy"":""Tester"",""Time"":0,""Score"":7,""CommentCount"":255}"));
    }

    [Test]
    public void DeserializeTest() {
        var json = @"{""by"":""Tester"",""descendants"":123,""id"":777,""kids"":[1,2,3,4],""score"":321,""time"":10,""title"":""Test2"",""type"":""story"",""url"":""https:/test/url.php""}";
        var story = JsonSerializer.Deserialize<HackerNewsStory>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        Assert.That(story, Is.Not.Null);
        Assert.That(story.Title,        Is.EqualTo("Test2"));
        Assert.That(story.Uri,          Is.EqualTo("https:/test/url.php"));
        Assert.That(story.PostedBy,     Is.EqualTo("Tester"));
        Assert.That(story.Time,         Is.EqualTo(DateTimeOffset.UnixEpoch.ToUnixTimeSeconds() + 10));
        Assert.That(story.Score,        Is.EqualTo(321));
        Assert.That(story.CommentCount, Is.EqualTo(123));
    }
}
