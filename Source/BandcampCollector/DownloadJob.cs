using BandcampCollector.Shared.Jobs;

namespace BandcampCollector
{
    internal class DownloadJob : IJobConsumer<BandcampCollectionItem>
    {
        private readonly CollectionItemDownloader _downloader;

        public DownloadJob(CollectionItemDownloader downloader)
        {
            _downloader = downloader;
        }

        private static volatile int count = 0;

        public async Task<BandcampCollectionItem> ConsumeAsync()
        {
            Console.WriteLine(Interlocked.Increment(ref count));

            //await _downloader.RetrieveDigitalItemAsync();
            //await _downloader.DownloadBandcampCollectionItemAsync();

            return _downloader.BandcampCollectionItem;
        }
    }
}
