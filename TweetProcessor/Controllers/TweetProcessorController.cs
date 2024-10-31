using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace TweetProcessor.Controllers
{
    [ApiController]
    [Route("tweets")]
    public class TweetProcessor : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<TweetProcessor> _logger;

        public TweetProcessor(DaprClient daprClient, ILogger<TweetProcessor> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessTweet([FromBody] Tweet tweet)
        {
            // Save Tweet to MongoDB using State API
            await _daprClient.SaveStateAsync("sentiment-scorer", tweet.Id, tweet);
            _logger.LogInformation($"Saved tweet with id {tweet.Id}");

            // Call Sentiment Scorer to analyze the sentiment of the tweet
            var result = await _daprClient.InvokeMethodAsync<Tweet, SentimentScore>(
                HttpMethod.Post,
                "sentiment-scorer",
                "sentiment",
                tweet);

            _logger.LogInformation($"Analyzed sentiment for tweet {tweet.Id}: Score: {result.Score}");

            // Publish to Redis Pub/Sub for processed tweets
            await _daprClient.PublishEventAsync("processed-tweets-pubsub", "processed-tweets", result);
            _logger.LogInformation($"Published scored tweet {tweet.Id}");

            return Ok();
        }
    }
}
