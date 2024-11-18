﻿using Quartz;
using Quartz.Spi;

namespace TweetProvider.Jobs
{
    public class PublishEventJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public PublishEventJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
