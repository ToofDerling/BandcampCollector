using System.Collections.Concurrent;

namespace BandcampCollector.Shared.JobQueue
{
    public interface IJobProducer<T>
    {
        Task ProduceAsync(BlockingCollection<IJobConsumer<T>> jobQueue);
    }
}
