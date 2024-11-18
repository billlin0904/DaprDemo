using Dapr.Client;
using Quartz;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using TweetProvider.Controllers;
using TweetProvider.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace TweetProvider.Jobs
{
    public class PublishEventJob : IJob
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PublishEventJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PublishEventJob(DaprClient daprClient, IServiceProvider serviceProvider, ILogger<PublishEventJob> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _serviceProvider.CreateScope();

            var dbContexts = scope.ServiceProvider.GetRequiredService<PublishEventDbContext>();
            // 從 JobDataMap 中獲取參數
            var dataMap = context.MergedJobDataMap;

            var topicName = dataMap.GetString("TopicName");
            var pubsubName = dataMap.GetString("PubSubName");

            var eventPayload = dataMap.GetString("EventPayload");

            Console.WriteLine($"Publishing event to topic: {topicName}");

            var tweet = JsonSerializer.Deserialize<Tweet>(eventPayload);

            // 使用 DaprClient 發布事件
            await _daprClient.PublishEventAsync(pubsubName, topicName, tweet);
            _logger.LogInformation($"Published scored tweet {tweet.Id}");

            var entity = dbContexts.PublishEventJobs.First(x => x.JobName == tweet.Id && x.JobGroup == "tweets");
            entity.State = 2; // 2 代表已完成
            await dbContexts.SaveChangesAsync();
        }
    }
}
