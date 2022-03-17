using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTBatcher
{
    public class YTPlaylist
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string nextPageToken { get; set; }
        public YTPlaylistItem[] items { get; set; }
        public YTPlaylistPageInfo pageInfo { get; set; }
    }

    public class YTPlaylistItem
    {
        public string kind { get; set; }
        public string etag { get; set; }
        public string id { get; set; }
        public YTPlaylistItemSnippet snippet { get; set; }
    }

    public class YTPlaylistPageInfo
    {
        public int totalResults { get; set; }
        public int resultsPerPage { get; set; }
    }
    public class YTPlaylistItemSnippet
    {
        public string title { get; set; }
        public YTPlaylistItemResourceId resourceId { get; set; }
    }

    public class YTPlaylistItemResourceId
    {
        public string kind { get; set; }
        public string videoId { get; set; }
    }
}
