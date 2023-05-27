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

        public async Task<BandcampCollectionItem> ConsumeAsync()
        {
            await _downloader.RetrieveDigitalItemAsync();
            await _downloader.DownloadBandcampCollectionItemAsync();

            return _downloader.BandcampCollectionItem;
        }
    }
}
