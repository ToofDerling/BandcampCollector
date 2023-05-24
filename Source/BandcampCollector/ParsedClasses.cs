using System.Text.Json.Serialization;

namespace BandcampCollector
{
    public sealed class ParsedBandcampData
    {
        public List<ParsedDigitalItem> digital_items { get; set; }
    }

    public sealed class ParsedDigitalItem
    {
        public Dictionary<string, Dictionary<string, string>> downloads { get; set; }

        public string? package_release_date { get; set; }

        public string title { get; set; }

        public string artist { get; set; }

        public string download_type { get; set; }

        public string download_type_str { get; set; }

        public string item_type { get; set; }

        public long? art_id { get; set; }
    }

    public sealed class ParsedFanPageData
    {
        public ParsedFanData fan_data { get; set; }

        public ParsedCollectionData collection_data { get; set; }

        public ParsedCollectionData hidden_data { get; set; }

        public ParsedItemCache item_cache { get; set; }
    }


    public sealed class ParsedCollectionItem
    {
        public long sale_item_id { get; set; }

        public string band_name { get; set; }

        public string item_title { get; set; }

        [JsonIgnore]
        public KeyValuePair<string, string> RedownloadUrl { get; set; }
    }

    public sealed class ParsedCollectionItems
    {
        public List<ParsedCollectionItem> items { get; set; }

        public bool more_available { get; set; }

        public string last_token { get; set; }

        public Dictionary<string, string> redownload_urls { get; set; }
    }

    public sealed class ParsedFanData
    {
        public long fan_id { get; set; }
    }

    public sealed class ParsedItemCache
    {
        public Dictionary<string, ParsedCollectionItem> collection { get; set; }

        public Dictionary<string, ParsedCollectionItem> hidden { get; set; }
    }

    public sealed class ParsedCollectionData
    {
        public int batch_size { get; set; }

        public int item_count { get; set; }

        public string last_token { get; set; }

        public Dictionary<string, string> redownload_urls { get; set; }
    }
}
