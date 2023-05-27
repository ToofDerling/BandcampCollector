﻿using System.Collections.Concurrent;

namespace BandcampCollector.Shared.Jobs
{
    public abstract class AbstractJobQueue<T>
    {
        private class InternalJobWaiter : JobWaiter
        {
            public void SignalWaitIsOver()
            {
                _waitingQueue.Add("Stop");
            }
        }

        private InternalJobWaiter _jobWaiter;

        protected AbstractJobQueue(int numWorkerThreads = 1)
        {
            _numWorkerThreads = numWorkerThreads;
        }

        protected BlockingCollection<IJobConsumer<T>> _jobQueue;

        private readonly int _numWorkerThreads;

        private volatile int _numFinishedWorkerThreads = 0;

        protected JobWaiter InitQueueWaiterAndWorkerThreads(bool withWaiter)
        {
            _jobQueue = new BlockingCollection<IJobConsumer<T>>();

            if (withWaiter)
            {
                _jobWaiter = new InternalJobWaiter();
            }

            for (int i = 0; i < _numWorkerThreads; i++)
            {
                Task.Factory.StartNew(JobExecutorLoopAsync, TaskCreationOptions.LongRunning);
            }

            return _jobWaiter;
        }

        public void Stop()
        {
            _jobQueue.CompleteAdding();
        }

        public void AddJob(IJobConsumer<T> job)
        {
            _jobQueue.Add(job);
        }

        private async Task JobExecutorLoopAsync()
        {
            foreach (var job in _jobQueue.GetConsumingEnumerable())
            {
                var result = await job.ConsumeAsync();

                JobExecuted?.Invoke(this, new JobEventArgs<T>(result));
            }

            if (Interlocked.Increment(ref _numFinishedWorkerThreads) == _numWorkerThreads)
            {
                _jobWaiter?.SignalWaitIsOver();

                _jobQueue.Dispose();
            }
        }

        public event EventHandler<JobEventArgs<T>> JobExecuted;
    }
}