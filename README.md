# Hacker News API

Written in C# ASP.NET Core 8.0

## Building and running

Download .NET 8.0 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

To build the API:
```sh
    cd Api
    dotnet build --configuration Release
```

To run the API server after successful build execute from `Api` directory:
```sh
    ./bin/Debug/net8.0/Api
```

## Endpoint

There is just one endpoint: `/best?n=` which accepts the integer in the range `<1; 200>`.
It will fetch the `n` best stories from Hacker News [website](https://news.ycombinator.com/)
and return them sorted by score in descending order with details.

The default value for `n` is `10`;

The response is in JSON in the form:
```json
    [
        {
            "title":        "A uBlock Origin update was rejected from the Chrome Web Store",
            "uri":          "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            "postedBy":     "ismaildonmez",
            "time":         "2019-10-12T13:43:01+00:00",
            "score":        1716,
            "commentCount": 572
        },
        { ... },
        { ... },
        { ... },
        ...
    ]
```

Example API calls:
```
    http://localhost:5000/best
    http://localhost:5000/best?n=1
    http://localhost:5000/best?n=101
    http://localhost:5000/best?n=200
```

You can use Tester console application from this repository
for examples on how to call the API and get formatted JSON response.


## Assumptions

The main assumption is that the maximum amount of best stories ids returned by
the `beststories.json` endpoint is 200.  That behaviour is not documented but
I've tested it thoroughly.

Based on that assumption a lot of collection have pre allocated sizes and we
always fetch all the best stories which allows us to serve all the possible
API calls in constant time - no matter if you ask for just one or for 200
stories - all the data is already fetched.

The downsize of that is that the data you are getting from this API might be
old.  How old?  It depends how long it takes to fetch it from the HackerNews
API which is quite inconsistent and sometimes the data fetch takes less then
a second and other times it takes a couple of seconds.  Typically it is around
1-2 seconds tho.  So taking that into account - and the delay between data
fetches which is coded to 1 second - the data you fetch from the `/best`
endpoint will be typically 2 to 3 seconds old.

Which for the discussion aggregation website is totally fine in my opinion as
the only thing that changes the output of the `/beststories` API are the user
posts and interactions - and humans are quite slow.

## Possible enhancements

The next thing which I would do would be to profile the code.  Also I would set
up a proper testing environment to do proper throughput and latency tests and to
establish a base performance metrics on which I could base further changes.

Additionally to profiling the server code I would look into the network 'cause
there is something weird going one with how inconsistent are response times
from the HackerNews API and I would like to have them improved when fetching data.

In the code itself I would also make it so that the API calls which ask just
for 1 or two stories don't have to wait for the entire fetch of the 200
stories.  Right now for the simplicity all the stories are fetched and only
after all the stories are fetched they are made available for the clients
calling the API (see the `_stories = stories;` line in the `HackerNewsService.cs`).
That was easy to work with but that's something which could be improved.

Rest of the implementation changes would be dictated by the profiling outcome.

