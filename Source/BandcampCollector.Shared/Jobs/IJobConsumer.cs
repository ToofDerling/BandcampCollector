namespace BandcampCollector.Shared.Jobs
{
    public interface IJobConsumer<T>
    {
        Task<T> ConsumeAsync();
    }
}
