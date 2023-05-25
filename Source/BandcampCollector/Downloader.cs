using BandcampCollector.Shared.IO;
using System.Globalization;

namespace BandcampCollector
{
    public class Downloader
    {
        private readonly HttpClient _httpClient;

        private static readonly object _consoleLock = new();

        private static volatile int _downloaderCount = 0;

        private readonly int _consoleRow;
        private readonly int _totalDownloads;
        private readonly int _downloaderId;

        public Downloader(HttpClient httpClient, int cursorTop, int totalDownloads)
        {
            _httpClient = httpClient;

            _downloaderId = Interlocked.Increment(ref _downloaderCount);
            _consoleRow = cursorTop + (_downloaderId - 1);

            _totalDownloads = totalDownloads;
        }

        private static string GetReleaseString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "Unknown";
            }

            if (Settings.FixArtistTitleCasing)
            {
                if (str.IsUpperCased())
                {
                    return CaseValidation.FixCasedString(str, isLowerCased: false);
                }
                if (str.IsLowerCased())
                {
                    return CaseValidation.FixCasedString(str, isLowerCased: true);
                }
            }

            return str;
        }

        public async Task DownloadItemAsync(ParsedCollectionItem collectionItem, ParsedDigitalItem? digitalItem)
        {
            var pre = $"{_downloaderId}/{_totalDownloads} ";

            // Releasename

            var title = GetReleaseString(collectionItem.item_title);
            var artist = GetReleaseString(collectionItem.band_name);

            var releaseName = $"{artist} - {title} ";

            // Downloadurl

            var downloadUrl = GetDownloadUrl(digitalItem);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                lock (_consoleLock)
                {
                    PrintRelease(pre, releaseName, string.Empty, Console.ForegroundColor);
                    Console.WriteLine("no digital download");
                }
                return;
            }

            // Downloadsize

            var downloadMap = digitalItem.downloads[Settings.AudioFormat]; // When we have the downloadUrl this won't fail
            if (!downloadMap.TryGetValue("size_mb", out var downloadSize))
            {
                downloadSize = "?MB";
            }

            // Releaseyear

            var releaseYear = "0000";
            if (DateTimeOffset.TryParseExact(digitalItem.package_release_date, "dd MMM yyyy HH:mm:ss Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseUtc))
            {
                releaseYear = releaseUtc.Year.ToString();
            }

            var releaseInfo = $"({releaseYear}, {downloadSize}";

            // Pre-order

            var isPreOrder = releaseUtc > DateTimeOffset.Now; // Default value is DateTimeOffset.MinValue
            if (isPreOrder)
            {
                releaseInfo += $", pre-order: {releaseUtc:dd-MM-yyyy}";
            }
            releaseInfo += ") ";

            lock (_consoleLock)
            {
                PrintRelease(pre, releaseName, releaseInfo, Settings.WorkingColor);
                Console.WriteLine();
            }

            // Single track (TODO)
            var isSingleTrack = digitalItem.download_type == "t" || digitalItem.download_type_str == "track" || digitalItem.item_type == "track";

            var state = string.Empty;
            try
            {
                // Prepare artist folder
                var artistFolder = Path.Combine(Settings.DownloadFolder, artist);
                state = artistFolder;

                if (!Directory.Exists(artistFolder))
                {
                    Directory.CreateDirectory(artistFolder);
                }

                // Prepare filename
                var downloadFileName = $"{releaseYear} - {title}";
                if (isPreOrder)
                {
                    downloadFileName += " (pre-order)";
                }
                downloadFileName += ".tmp";
                state = downloadFileName;

                var downloadPath = Path.Combine(artistFolder, downloadFileName);

                // Download
                using (var fileStream = AsyncStreams.AsyncFileWriteStream(downloadPath))
                {
                    state = "connecting";
                    lock (_consoleLock)
                    {
                        PrintRelease(pre, releaseName, releaseInfo, Settings.WorkingColor);
                        Console.WriteLine(state);
                    }

                    using var downloadStream = await _httpClient.GetStreamAsync(downloadUrl);

                    state = "downloading";
                    lock (_consoleLock)
                    {
                        PrintRelease(pre, releaseName, releaseInfo, Settings.WorkingColor);
                        Console.WriteLine(state);
                    }

                    await downloadStream.CopyToAsync(fileStream);
                }

                // Finalize download
                var releaseFileName = Path.ChangeExtension(downloadFileName, ".zip");
                state = releaseFileName;

                var releasePath = Path.Combine(artistFolder, releaseFileName);
                File.Move(downloadPath, releasePath, overwrite: true);

                lock (_consoleLock)
                {
                    PrintRelease(pre, releaseName, releaseInfo, Settings.OkColor);
                    Console.WriteLine("downloaded "); // Need the last space to overwrite "downloading"
                }
            }
            catch (Exception ex)
            {
                lock (_consoleLock)
                {
                    PrintRelease(pre, releaseName, releaseInfo, Settings.ErrorColor);
                    PrintError(state, ex);
                    Console.WriteLine();
                }
            }
        }

        private static string? GetDownloadUrl(ParsedDigitalItem? digitalItem)
        {
            // Some releases have no digital items (eg. vinyl only) or no downloads or no urls, so we return null in such cases
            if (digitalItem == null)
            {
                return null;
            }

            var downloads = digitalItem.downloads;
            if (downloads == null || downloads.Count == 0)
            {
                return null;
            }

            var download = downloads[Settings.AudioFormat];
            if (download == null || download.Count == 0)
            {
                return null;
            }

            var downloadUrl = download["url"];
            return downloadUrl;
        }

        private void PrintRelease(string pre, string releaseName, string releaseInfo, ConsoleColor consoleColor)
        {
            Console.SetCursorPosition(0, _consoleRow);
            Console.Write(pre);

            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = consoleColor;
            Console.Write(releaseName);

            Console.ForegroundColor = oldColor;

            if (!string.IsNullOrEmpty(releaseInfo)) 
            {
                Console.Write(releaseInfo);
            }
        }

        private static void PrintError(string error, Exception ex)
        {
            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = Settings.ErrorColor;
            Console.Write($"{error} {ex.GetType().Name}");

            Console.ForegroundColor = oldColor;
        }

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

    }
}
