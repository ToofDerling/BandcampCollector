using CollectionManager;

namespace BandcampCollector
{
    public class BandcampCollectionItem : CollectionItem
    {
        public string PaymentIdParam { get; set; }

        public string SigParam { get; set; }

        public string GetRedownloadPageUrl()
        {
            return $"https://bandcamp.com/download?from=collection&payment_id={PaymentIdParam}&sig={SigParam}&sitem_id={Id}";
        }
    }
}
