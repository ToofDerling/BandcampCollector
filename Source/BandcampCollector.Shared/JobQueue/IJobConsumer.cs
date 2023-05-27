namespace BandcampCollector.Shared.JobQueue
{
    public interface IJobConsumer<T>
    {
        Task<T> ConsumeAsync();
    }
}
