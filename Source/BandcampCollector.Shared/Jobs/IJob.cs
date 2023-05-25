namespace BandcampCollector.Shared.Jobs
{
    public interface IJob<T>
    {
        T Execute();
    }
}
