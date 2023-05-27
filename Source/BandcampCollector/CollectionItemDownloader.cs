using BandcampCollector.Shared.Extensions;
using BandcampCollector.Shared.IO;
using System.Text.Json;

namespace BandcampCollector
{
    public class CollectionItemDownloader
    {
        private static volatile int _downloaderCount = 0;

        private readonly int _consoleRow;

        public BandcampCollectionItem BandcampCollectionItem { get; private set; }

        private readonly ParsedCollectionItem _parsedCollectionItem;

        private readonly string pre;

        public CollectionItemDownloader(ParsedCollectionItem parsedCollectionItem, BandcampCollectionItem bandcampCollectionItem, int totalDownloads)
        {
            var downloaderId = Interlocked.Increment(ref _downloaderCount);
            _consoleRow = ConsoleWriter.GetCursorTop() + downloaderId;

            BandcampCollectionItem = bandcampCollectionItem;
            _parsedCollectionItem = parsedCollectionItem;

            pre = $"{downloaderId}/{totalDownloads} ";
        }

        public async Task RetrieveDigitalItemAsync()
        {
            ParsedDigitalItem? digitalItem = null;

            var saleItemUrl = _parsedCollectionItem.RedownloadUrl.Value;

            // Get redownloadpage content
            using var response = await HttpDownloader.Client.GetAsync(saleItemUrl);
            if (response.IsSuccessStatusCode)
            {
                // Get data blob
                var doc = await DocumentHelper.GetDocumentAsync(response);
                var data = DocumentHelper.GetDecodedDataBlob(doc);

                var parsedData = JsonSerializer.Deserialize<ParsedBandcampData>(data);
                digitalItem = parsedData.digital_items[0];
            }

            CollectionMapper.MapBandcampCollectionItem(BandcampCollectionItem, _parsedCollectionItem, digitalItem);
        }

        public async Task DownloadBandcampCollectionItemAsync()
        {
            var releaseInfo = " ";

            // Download url

            if (!BandcampCollectionItem.HasDownloadUrl)
            {
                ConsoleWriter.WriteAt(pre, BandcampCollectionItem.Name, releaseInfo, Console.ForegroundColor, _consoleRow, "no digital download");
                return;
            }

            var downloadUrl = BandcampCollectionItem.DownloadUrl(Settings.AudioFormat);
            var downloadFile = BandcampCollectionItem.Name;

            var releaseName = BandcampCollectionItem.Name;

            // Pre-order

            if (BandcampCollectionItem.IsPreOrder)
            {
                releaseInfo += $"(pre-order {BandcampCollectionItem.ReleaseUtc:dd-MM-yyyy}, ";
                downloadFile += " (pre-order).tmp";
            }
            else
            {
                releaseInfo += "(";
            }

            // Downloadsize

            if (!BandcampCollectionItem.DownloadSizes.TryGetValue(Settings.AudioFormat, out var downloadSize))
            {
                downloadSize = "?MB";
            }

            releaseInfo += $"{downloadSize}) ";
            ConsoleWriter.WriteAt(pre, releaseName, releaseInfo, Settings.WorkingColor, _consoleRow, string.Empty);

            // Single track (TODO)

            if (BandcampCollectionItem.IsSingleTrack)
            {
                // Grab artwork right? (see bandcamp-collection-downloader)
            }

            var state = string.Empty;
            try
            {
                downloadFile += ".tmp";
                state = downloadFile;

                var downloadPath = Path.Combine(Settings.DownloadFolder, downloadFile.ToFileSystemString());

                // Download
                using (var fileStream = AsyncStreams.AsyncFileWriteStream(downloadPath))
                {
                    state = "connecting";
                    ConsoleWriter.WriteAt(pre, releaseName, releaseInfo, Settings.WorkingColor, _consoleRow, state);

                    using var downloadStream = await HttpDownloader.Client.GetStreamAsync(downloadUrl);

                    state = "downloading";
                    ConsoleWriter.WriteAt(pre, releaseName, releaseInfo, Settings.WorkingColor, _consoleRow, state);

                    await downloadStream.CopyToAsync(fileStream);
                }

                // Finalize download
                var releaseFileName = Path.ChangeExtension(downloadFile, ".zip");
                state = releaseFileName;

                var releasePath = Path.Combine(Settings.DownloadFolder, releaseFileName);
                File.Move(downloadPath, releasePath, overwrite: true);

                ConsoleWriter.WriteAt(pre, releaseName, releaseInfo, Settings.OkColor, _consoleRow, "downloaded "); // Need the last space to overwrite "downloading"
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteAt(pre, releaseName, releaseInfo, Settings.ErrorColor, _consoleRow, string.Empty, state, ex);
            }
        }

        #region RetrieveRealDownloadURL from bandcamp-collection-downloader
        /*
        private async Task<string?> RetrieveRealDownloadURL( string saleItemID, string audioFormat) 
        {
        val random = Random()

        // Construct statdownload request URL
        val statdownloadURL: String = downloadUrl
            .replace("/download/", "/statdownload/")
            .replace("http:", "https:") + "&.vrs=1" + "&.rand=" + random.nextInt()

        // Get statdownload JSON
        val statedownloadUglyBody: String = util.retry({
            Jsoup.connect(statdownloadURL)
                .cookies(cookies)
                .timeout(timeout)
                .get().body().select("body")[0].text().toString()
        }, retries)!!

        val prefixPattern = Pattern.compile("""if\s*\(\s*window\.Downloads\s*\)\s*\{\s*Downloads\.statResult\s*\(\s*""")
        val suffixPattern = Pattern.compile("""\s*\)\s*};""")
        val statdownloadJSON: String =
            prefixPattern.matcher(
                suffixPattern.matcher(statedownloadUglyBody)
                    .replaceAll("")
            ).replaceAll("")

        // Parse statdownload JSON

        val statdownloadParsed = gson.fromJson(statdownloadJSON, ParsedStatDownload::class.java)

        return statdownloadParsed.download_url?: downloadUrl
}
        */
        #endregion
    }
}
