using Dapr.Client;
using Quartz;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using TweetProvider.Controllers;

namespace TweetProvider.Jobs
{
    public class PublishEventJob : IJob
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PublishEventJob> _logger;

        public PublishEventJob(DaprClient daprClient, ILogger<PublishEventJob> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
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
        }
    }
}
