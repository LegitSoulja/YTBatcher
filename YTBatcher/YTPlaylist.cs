using System.Text.Json;
using System.Text.Json.Serialization;

namespace YTBatcher
{
    public class YTPlaylist
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("etag")]
        public string ETag { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string NextPageToken { get; set; }

        [JsonPropertyName("items")]
        public YTPlaylistItem[] Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public YTPlaylistPageInfo PageInfo { get; set; }
    }

    public class YTPlaylistItem
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("etag")]
        public string ETag { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("snippet")]
        public YTPlaylistItemSnippet Snippet { get; set; }
    }

    public class YTPlaylistPageInfo
    {
        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }

        [JsonPropertyName("resultsPerPage")]
        public int ResultsPerPage { get; set; }
    }
    public class YTPlaylistItemSnippet
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("resourceId")]
        public YTPlaylistItemResourceId ResourceId { get; set; }
    }

    public class YTPlaylistItemResourceId
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("videoId")]
        public string VideoId { get; set; }
    }
}
