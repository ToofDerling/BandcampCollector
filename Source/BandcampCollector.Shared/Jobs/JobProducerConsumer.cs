using System.Collections.Concurrent;

namespace BandcampCollector.Shared.Jobs
{
    public class JobProducerConsumer<T>
    {
        private class InternalJobWaiter : JobWaiter
        {
            public void SignalWaitIsOver()
            {
                _waitingQueue.Add("Stop");
            }
        }

        private BlockingCollection<IJobConsumer<T>> _jobQueue;

        private InternalJobWaiter _jobWaiter;

        private readonly int _numWorkerThreads;

        private int _numFinishedThreads = 0;

        public JobProducerConsumer(int numWorkerThreads = 1)
        {
            _numWorkerThreads = numWorkerThreads;
        }

        public JobWaiter Start(IJobProducer<T> producer, bool withWaiter)
        {
            _jobQueue = new BlockingCollection<IJobConsumer<T>>();

            if (withWaiter)
            {
                _jobWaiter = new InternalJobWaiter();
            }

            for (int i = 0; i < _numWorkerThreads; i++)
            {
                Task.Factory.StartNew(ConsumerLoopAsync, TaskCreationOptions.LongRunning);
            }

            Task.Factory.StartNew(() => producer.ProduceAsync(_jobQueue), TaskCreationOptions.LongRunning);

            return _jobWaiter;
        }

        private async Task ConsumerLoopAsync()
        {
            foreach (var job in _jobQueue.GetConsumingEnumerable())
            {
                var result = await job.ConsumeAsync();
                
                JobExecuted?.Invoke(this, new JobEventArgs<T>(result));
            }

            if (Interlocked.Increment(ref _numFinishedThreads) == _numWorkerThreads)
            {
                _jobWaiter?.SignalWaitIsOver();

                _jobQueue.Dispose();
            }
        }

        public event EventHandler<JobEventArgs<T>> JobExecuted;
    }
}
