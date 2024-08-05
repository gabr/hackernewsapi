namespace Api;

public class HackerNewsStory {
    // the fields we return when serializing to JSON
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
}
