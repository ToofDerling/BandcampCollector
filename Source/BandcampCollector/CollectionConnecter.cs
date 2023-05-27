using Fizzler.Systems.HtmlAgilityPack;
using System.Net;
using System.Text.Json;
using BrowserCookiesGrabber;
using BandcampCollector.Shared.Extensions;
using BandcampCollector.Shared.Helpers;
using BandcampCollector.Shared.Jobs;

namespace BandcampCollector
{
    public class CollectionConnecter
    {
        public async Task MapCollectionAsync()
        {
            Console.CursorVisible = false;

            var cookieGrabber = new CookiesGrabber();
            var browserCookies = await cookieGrabber.GrabCookiesAsync(".bandcamp.com");

            ParsedFanPageData parsedFanPage = null;

            foreach (var browser in browserCookies)
            {
                Console.WriteLine($"Using cookies from {browser.Browser.Name}");

                HttpDownloader.CreateHttpClient(browser.Cookies);
                try
                {
                    // If the cookies are invalid this will fail with a json parsing exception 
                    // because the needed data is not available on a logged out fanpage.
                    parsedFanPage = await GetFanPageDataAsync(Settings.BandcampUser);
                    break;
                }
                catch (JsonException)
                {
                    HttpDownloader.Client.DisposeDontCare();
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
                //Console.WriteLine("Skipping hidden items");
                FilterHiddenItems(parsedFanPage);
            }

            var jobProducerConsumer = new JobProducerConsumer<BandcampCollectionItem>(numWorkerThreads: Settings.ParallelDownloads);

            ConsoleWriter.SetCursorTop();
            var producer = new CollectionBatchRetriever(parsedFanPage);

            var jobWaiter = jobProducerConsumer.Start(producer, withWaiter: true);
            jobWaiter.WaitForJobsToFinish();

            Console.WriteLine("Done");
            Quit();
        }

        private static void Quit()
        {
            HttpDownloader.Client.DisposeDontCare();
            Console.CursorVisible = true;
            Console.ReadLine();
        }

        private static async Task<ParsedFanPageData> GetFanPageDataAsync(string bandcampUser)
        {
            var url = $"https://bandcamp.com/{bandcampUser}";

            Console.WriteLine($"Connecting to {url}");

            using var response = await HttpDownloader.Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var doc = await DocumentHelper.GetDocumentAsync(response);
            var data = DocumentHelper.GetDecodedDataBlob(doc);

            // If cookies are invalid connection to the fanpage works, but json parsing will fail because the needed
            // data is not available on a logged out fanpage. So don't display a "green" connection until after json
            // parsing has succeded.
            var parsedData = JsonSerializer.Deserialize<ParsedFanPageData>(data);

            var titleTag = doc.QuerySelector("head title");
            var title = WebUtility.HtmlDecode(titleTag.InnerText);
            ProgressReporter.Done(title);

            var total = parsedData.collection_data.item_count;
            Console.WriteLine($"Found {total} items");

            return parsedData;
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
    }
}
