using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api;

public class HackerNewsStory {
    // the fields we return when serializing to JSON
    [JsonIgnore]
    public int     Id           { get; init; }
    public string? Title        { get; init; }
    public string? Uri          => Url;
    public string? PostedBy     => By;
    public long    Time         { get; init; }
    public int     Score        { get; init; }
    public int     CommentCount => Descendants;

    // original field names when deserializing JSON
    public string? By          { private get; set; }
    public int     Descendants { private get; set; }
    public string? Url         { private get; set; }

    // serialize to JSON only once to to repeate the work
    // for other connections asking for the same data
    private string? _json = null;
    public string GetJson() {
        if (_json == null)
            _json = JsonSerializer.Serialize(this);
        return _json;
    }
}
