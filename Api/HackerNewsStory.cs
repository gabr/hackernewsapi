using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api;

/// <summary>
/// For serialization and deserialization of the HackerNews story details.
/// Some of the fields are used during deserialization and others for serialization.
/// It's somehow convoluted but I wanted to avoid the need to either
/// have two separate classes and allocate twice as much or
/// to have to create a custom complex JSON converter.
/// </summary>
public class HackerNewsStory {
    // the fields we return when serializing to JSON
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

    // serialize to JSON only once to not to repeat the work
    // for other connections asking for the same data
    private string? _json = null;
    public string ToJson() {
        if (_json == null)
            _json = JsonSerializer.Serialize(this);
        return _json;
    }
}
