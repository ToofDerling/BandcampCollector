using System.Collections.Concurrent;

namespace BandcampCollector.Shared.Jobs
{
    public interface IJobProducer<T>
    {
        Task ProduceAsync(BlockingCollection<IJobConsumer<T>> jobQueue);
    }
}
