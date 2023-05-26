using BandcampCollector.Shared.StringCasing;
using System.Globalization;
using System.Web;

namespace BandcampCollector
{
    public class CollectionMapper
    {
        public static List<ParsedCollectionItem> GetCollectionBatchWithRedownloadUrls(ICollection<ParsedCollectionItem> collectionItemBatch, 
            Dictionary<string, string> redownloadUrlBatch)
        {
            var mappedCollectionItems = new List<ParsedCollectionItem>(collectionItemBatch.Count);

            foreach (var collectionItem in collectionItemBatch)
            {
                var saleItemIdStr = collectionItem.sale_item_id.ToString();

                var redownloadUrl = redownloadUrlBatch.First(url => url.Key[1..] == saleItemIdStr);
                collectionItem.RedownloadUrl = redownloadUrl;

                mappedCollectionItems.Add(collectionItem);
            }

            return mappedCollectionItems;
        }

        public static BandcampCollectionItem CreateBandcampCollectionItem(ParsedCollectionItem parsedCollectionItem)
        {
            var id = parsedCollectionItem.sale_item_id.ToString();
            var query = HttpUtility.ParseQueryString(parsedCollectionItem.RedownloadUrl.Value);

            var bandcampCollectionItem = new BandcampCollectionItem
            {
                Id = id,
                PaymentIdParam = query["payment_id"],
                SigParam = query["sig"],
            };

            return bandcampCollectionItem;
        }

        public static void MapBandcampCollectionItem(BandcampCollectionItem bandcampCollectionItem, ParsedCollectionItem parsedCollectionItem, ParsedDigitalItem? parsedDigitalItem)
        {
            var band = parsedCollectionItem.band_name;
            var title = parsedCollectionItem.item_title;

            if (Settings.FixBandTitleCasing)
            {
                band = FixCasing(band);
                title = FixCasing(title);
            }

            if (parsedDigitalItem == null)
            {
                bandcampCollectionItem.Name = $"{band} - {title}";
                return;
            }

            // Releaseyear
            var releaseYear = "0000";
            if (DateTimeOffset.TryParseExact(parsedDigitalItem.package_release_date, "dd MMM yyyy HH:mm:ss Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseUtc))
            {
                releaseYear = releaseUtc.Year.ToString();

                bandcampCollectionItem.ReleaseUtc = releaseUtc;
            }

            // Finalize the name
            bandcampCollectionItem.Name = $"{band} - {releaseYear} - {title}";

            // Donwload url and download sizes
            var firstUrl = true;
            foreach (var downloadMap in parsedDigitalItem.downloads)
            {
                if (downloadMap.Value.TryGetValue("url", out var url) && !string.IsNullOrEmpty(url))
                {
                    var audioFormat = downloadMap.Key;

                    if (firstUrl)
                    {
                        firstUrl = false;
                        url = url.Replace($"={audioFormat}", "={}");

                        bandcampCollectionItem.DownLoadUrlTemplate = url;
                        bandcampCollectionItem.DownloadSizes = new Dictionary<string, string>();
                    }

                    var mb = downloadMap.Value["size_mb"];
                    bandcampCollectionItem.DownloadSizes[audioFormat] = mb;
                }
            }

            // Single track (TODO)
            bandcampCollectionItem.IsSingleTrack = parsedDigitalItem.download_type == "t" || parsedDigitalItem.download_type_str == "track" || parsedDigitalItem.item_type == "track";

            static string FixCasing(string str)
            {
                if (str.IsUpperCased())
                {
                    return CaseValidation.FixCasedString(str, isLowerCased: false);
                }
                if (str.IsLowerCased())
                {
                    return CaseValidation.FixCasedString(str, isLowerCased: true);
                }
                return str;
            }
        }
    }
}
