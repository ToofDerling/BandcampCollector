using CollectionManager;

namespace BandcampCollector
{
    public class BandcampCollectionItem : CollectionItem
    {
        public string PaymentIdParam { get; set; }

        public string SigParam { get; set; }

        public string RedownloadPageUrl => $"https://bandcamp.com/download?from=collection&payment_id={PaymentIdParam}&sig={SigParam}&sitem_id={Id}";

        public DateTimeOffset? ReleaseUtc { get; set; }

        public bool IsPreOrder => ReleaseUtc.HasValue && ReleaseUtc.Value > DateTimeOffset.Now;

        public bool IsSingleTrack { get; set; }

        public string DownLoadUrlTemplate { get; set; }

        public string DownloadUrl(string audioFormat) => DownLoadUrlTemplate.Replace("={}", $"={audioFormat}");

        public bool HasDownloadUrl => !string.IsNullOrEmpty(DownLoadUrlTemplate);

        public Dictionary<string, string> DownloadSizes { get; set; }
    }
}
