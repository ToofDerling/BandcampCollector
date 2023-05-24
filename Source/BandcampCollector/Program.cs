using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text;
using BrowserCookiesGrabber;

namespace BandcampCollector
{
    public class Program
    {
        private static HttpClient HttpClient { get; set; }

        private static void CreateHttpClient(CookieContainer cookies)
        {
            var handler = new SocketsHttpHandler() { CookieContainer = cookies };
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromMinutes(60);

            HttpClient = client;
        }

        static async Task Main(string[] args)
        {
            Console.CursorVisible = false;

            var cookieGrabber = new CookiesGrabber();
            var browserCookies = await cookieGrabber.GrabCookiesAsync(".bandcamp.com");

            ParsedFanPageData parsedFanPage = null;

            foreach (var browser in browserCookies)
            {
                Console.WriteLine($"Using cookies from {browser.Browser.Name}");

                CreateHttpClient(browser.Cookies);
                try
                {
                    // If the cookies are invalid this will fail with a json parsing exception 
                    // because the needed data is not available on a logged out fanpage.
                    parsedFanPage = await GetFanPageDataAsync(Settings.BandcampUser);
                    break;
                }
                catch (JsonException)
                {
                    HttpClient.DisposeDisposable();
                }
            }

            if (parsedFanPage == null)
            {
                ProgressReporter.Error("Could not retrieve fanpage data");
                Quit();
                return;
            }

            if (Settings.SkipHiddenItems)
            {
                Console.WriteLine("Skipping hidden items");
                FilterHiddenItems(parsedFanPage);
            }

            var parsedCollectionItems = GetCollectionItemsFromFanPage(parsedFanPage);

            var bandcampCollectionItems = new List<BandcampCollectionItem>();

            foreach (var item in parsedCollectionItems)
            {
                bandcampCollectionItems.Add(new BandcampCollectionItem { Id = item.Value.sale_item_id.ToString() });
            }

            var fanId = parsedFanPage.fan_data.fan_id;
            var collectionData = parsedFanPage.collection_data;
            var collectionType = "collection_items"; // Or "hidden_items"

            await RetrieveAdditionalCollectionItemsAsync(fanId, collectionData, parsedCollectionItems, collectionType);

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Settings.ParallelDownloads };
            var cursorTop = Console.CursorTop;

            //await Parallel.ForEachAsync(collectionItems, parallelOptions,
            //    async (saleItem, _) =>
            //        await DownloadAsync(saleItem, cursorTop, collectionItems.Count));

            foreach (var item in parsedCollectionItems)
            {
                await DownloadAsync(item, cursorTop, parsedCollectionItems.Count);
            }

            /*
            var importableBrowsers = await CookieGetters.Default.GetInstancesAsync(true);

            var uri = new Uri("https://politiken.dk");

            var opera = importableBrowsers.FirstOrDefault(b => b.SourceInfo.BrowserName.Contains("Opera Webkit"));

            //CookieGetters.Browsers.Chrome.

            var res = await opera.GetCookiesAsync(uri);
            */

            Console.WriteLine("Done");
            Quit();
        }

        private static void Quit()
        {
            HttpClient.DisposeDisposable();
            Console.CursorVisible = true;
            Console.ReadLine();
        }


        private static readonly object _lock = new();
        private static volatile int _count;

        private static async Task DownloadAsync(KeyValuePair<long, ParsedCollectionItem> saleItem, int cursorTop, int totalDownloads)
        {
            var id = Interlocked.Increment(ref _count);
            if (id < 20)
            {
                return;
            }

            var collectionItem = saleItem.Value;
            var digitalItem = await RetrieveDigitalItemAsync(collectionItem);


            if (digitalItem == null)
            {
                lock (_lock)
                {
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = Settings.ErrorColor;
                    Console.WriteLine($"{id} No digital item");
                    Console.ForegroundColor = old;
                }
            }
            else
            {
                var sb = new StringBuilder();
                if (collectionItem.band_name != digitalItem.artist)
                {
                    sb.AppendLine($"{id} {collectionItem.band_name} vs {digitalItem.artist}");
                }
                if (collectionItem.item_title != digitalItem.title)
                {
                    var str = sb.Length == 0 ? id.ToString() : " ".PadLeft(id.ToString().Length);
                    sb.AppendLine($"{str} {collectionItem.item_title} vs {digitalItem.title}");
                }
                if (sb.Length > 0)
                {
                    Console.Write(sb);
                }
            }

            var downloader = new Downloader(HttpClient, cursorTop, totalDownloads);
            await downloader.DownloadItemAsync(collectionItem, digitalItem);
        }

        //private static JsonSerializerOptions _jsonSerializerOptions = new()
        //{
        //    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault
        //};


        private static async Task<ParsedFanPageData> GetFanPageDataAsync(string bandcampUser)
        {
            var url = $"https://bandcamp.com/{bandcampUser}";

            Console.WriteLine($"Connecting to {url}");

            using var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var doc = await GetDocumentAsync(response);

            var data = GetDecodedDataBlob(doc);
            var parsedData = JsonSerializer.Deserialize<ParsedFanPageData>(data /*,_jsonSerializerOptions*/);

            // If cookies are invalid connection to the fanpage works, but json parsing will fail because the needed
            // data is not available on a logged out fanpage. So don't display a "green" connection until after json
            // parsing has succeded.
            var titleTag = doc.QuerySelector("head title");
            var title = WebUtility.HtmlDecode(titleTag.InnerText);
            ProgressReporter.Done(title);

            var collectionCount = parsedData.collection_data.item_count;
            Console.WriteLine($"Found {collectionCount} items");

            return parsedData;
        }

        private static async Task<HtmlNode> GetDocumentAsync(HttpResponseMessage response)
        {
            var page = await response.Content.ReadAsStringAsync();

            var html = new HtmlDocument();
            html.LoadHtml(page);

            var doc = html.DocumentNode;
            return doc;
        }

        private static string GetDecodedDataBlob(HtmlNode doc)
        {
            var pageData = doc.QuerySelector("#pagedata");
            var data = pageData.Attributes["data-blob"].Value;

            data = WebUtility.HtmlDecode(data);
            return data;
        }

        private static void FilterHiddenItems(ParsedFanPageData parsedFanpage)
        {
            var collection = parsedFanpage.collection_data.redownload_urls;

            var hidden = parsedFanpage.item_cache.hidden;
            var hiddenSaleItemIds = hidden.Select(item => item.Value.sale_item_id.ToString()).ToArray();

            var hiddenItems = collection.Keys.Where(key => hiddenSaleItemIds.Any(id => key.Contains(id)));
            foreach (var item in hiddenItems)
            {
                collection.Remove(item);
            }
        }

        private static Dictionary<long, ParsedCollectionItem> GetCollectionItemsFromFanPage(ParsedFanPageData parsedFanPageData)
        {
            var collectionItems = new Dictionary<long, ParsedCollectionItem>(parsedFanPageData.collection_data.item_count);

            MapRedownloadUrlsToCollectionItems(collectionItems, parsedFanPageData.item_cache.collection.Values, parsedFanPageData.collection_data.redownload_urls);

            return collectionItems;
        }

        private static void MapRedownloadUrlsToCollectionItems(Dictionary<long, ParsedCollectionItem> mappedCollectionItems, ICollection<ParsedCollectionItem> collectionItemBatch,
            Dictionary<string, string> redownloadUrlBatch)
        {
            foreach (var collectionItem in collectionItemBatch)
            {
                var saleItemIdStr = collectionItem.sale_item_id.ToString();

                var redownloadUrl = redownloadUrlBatch.First(url => url.Key[1..] == saleItemIdStr);
                collectionItem.RedownloadUrl = redownloadUrl;

                mappedCollectionItems.Add(collectionItem.sale_item_id, collectionItem);
            }
        }

        private static async Task RetrieveAdditionalCollectionItemsAsync(long fanId, ParsedCollectionData collectionData, Dictionary<long, ParsedCollectionItem> collectionItems,
            string collectionType)
        {
            // Display downloads links found on fan page
            ProgressReporter.ShowMessage($"Retrieving item data: {collectionItems.Count}/{collectionData.item_count}");

            var lastToken = collectionData.last_token;
            var moreAvailable = true;

            while (moreAvailable)
            {
                var payload = new StringContent($"{{\"fan_id\": {fanId}, \"older_than_token\": \"{lastToken}\"}}");

                // Append download pages from this api endpoint as well
                using var response = await HttpClient.PostAsync($"https://bandcamp.com/api/fancollection/1/{collectionType}", payload);
                response.EnsureSuccessStatusCode();

                var str = await response.Content.ReadAsStringAsync();
                var parsedCollectionData = JsonSerializer.Deserialize<ParsedCollectionItems>(str);

                //var parsedCollectionData = await response.Content.ReadFromJsonAsync<ParsedCollectionItems>();

                MapRedownloadUrlsToCollectionItems(collectionItems, parsedCollectionData.items, parsedCollectionData.redownload_urls);

                lastToken = parsedCollectionData.last_token;
                moreAvailable = parsedCollectionData.more_available;

                moreAvailable = false;

                ProgressReporter.ShowMessage($"Retrieving item data: {collectionItems.Count}/{collectionData.item_count}");
            }

            Console.WriteLine();
        }

        private static async Task<ParsedDigitalItem?> RetrieveDigitalItemAsync(ParsedCollectionItem collectionItem)
        {
            ParsedDigitalItem? digitalItem = null;

            var saleItemUrl = collectionItem.RedownloadUrl.Value;

            // Get redownloadpage content
            using var response = await HttpClient.GetAsync(saleItemUrl);
            if (response.IsSuccessStatusCode)
            {
                // Get data blob
                var doc = await GetDocumentAsync(response);
                var data = GetDecodedDataBlob(doc);

                var parsedData = JsonSerializer.Deserialize<ParsedBandcampData>(data /*, _jsonSerializerOptions*/);
                digitalItem = parsedData.digital_items[0];
            }

            return digitalItem;
        }
    }
}
