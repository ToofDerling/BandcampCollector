using BandcampCollector.Shared.Helpers;
using BandcampCollector.Shared.Jobs;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace BandcampCollector
{
    internal class CollectionBatchRetriever : IJobProducer<BandcampCollectionItem>
    {
        private readonly ParsedFanPageData _parsedFanPageData;

        private readonly string _collectionType;

        public CollectionBatchRetriever(ParsedFanPageData parsedFanPageData)
        {
            _parsedFanPageData = parsedFanPageData;

            _collectionType = "collection_items"; // Or "hidden_items"

            ConsoleWriter.SetCursorTop();
        }

        private BlockingCollection<IJobConsumer<BandcampCollectionItem>> _jobQueue;

        public async Task ProduceAsync(BlockingCollection<IJobConsumer<BandcampCollectionItem>> jobQueue)
        {
            _jobQueue = jobQueue;

            // First handle collection batch from fanpage
            var collectionItems = _parsedFanPageData.item_cache.collection.Values;
            var redownloadUrls = _parsedFanPageData.collection_data.redownload_urls;

            var collectionBatch = CollectionMapper.GetCollectionBatchWithRedownloadUrls(collectionItems, redownloadUrls);

            AddCollectionBatchToDownloadQueue(collectionBatch);

            // Then retrieve additional collection batches from api
            await RetrieveCollectionBatchesAsync(collectionBatch.Count);

            _jobQueue.CompleteAdding();
        }

        private void AddCollectionBatchToDownloadQueue(List<ParsedCollectionItem> collectionBatch)
        {
            foreach (var parsedCollectionItem in collectionBatch)
            {
                var bandcampCollectionItem = CollectionMapper.CreateBandcampCollectionItem(parsedCollectionItem);

                var total = _parsedFanPageData.collection_data.item_count;
                var downloader = new CollectionItemDownloader(parsedCollectionItem, bandcampCollectionItem, total);

                _jobQueue.Add(new DownloadJob(downloader));
            }
        }

        private async Task RetrieveCollectionBatchesAsync(int count)
        {
            var fanId = _parsedFanPageData.fan_data.fan_id;
            var lastToken = _parsedFanPageData.collection_data.last_token;
            var moreAvailable = true;

            var total = _parsedFanPageData.collection_data.item_count;
            ProgressReporter.ShowMessage($"Retrieving item data: {count}/{total}");

            while (moreAvailable)
            {
                var payload = new StringContent($"{{\"fan_id\": {fanId}, \"older_than_token\": \"{lastToken}\"}}");

                // Append download pages from this api endpoint as well
                using var response = await HttpDownloader.Client.PostAsync($"https://bandcamp.com/api/fancollection/1/{_collectionType}", payload);
                response.EnsureSuccessStatusCode();

                //var str = await response.Content.ReadAsStringAsync();
                //var parsedCollectionData = JsonSerializer.Deserialize<ParsedCollectionItems>(str);

                var parsedCollectionData = await response.Content.ReadFromJsonAsync<ParsedCollectionItems>();

                var collectionItems = parsedCollectionData.items;
                var redownloadUrls = parsedCollectionData.redownload_urls;

                var collectionBatch = CollectionMapper.GetCollectionBatchWithRedownloadUrls(collectionItems, redownloadUrls);

                AddCollectionBatchToDownloadQueue(collectionBatch);

                lastToken = parsedCollectionData.last_token;
                moreAvailable = parsedCollectionData.more_available;

                moreAvailable = false;

                count += collectionBatch.Count;
                ProgressReporter.ShowMessage($"Retrieving item data: {count}/{total}");
            }

            Console.WriteLine();
        }
    }
}
