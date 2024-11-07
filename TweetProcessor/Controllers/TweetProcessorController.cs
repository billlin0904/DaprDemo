using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace TweetProcessor.Controllers
{
    [ApiController]
    public class TweetProcessorController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<TweetProcessorController> _logger;

        public TweetProcessorController(DaprClient daprClient, ILogger<TweetProcessorController> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        [Topic("tweets-pubsub", "tweets")]
        [HttpPost("/tweets")]
        public async Task<IActionResult> ProcessTweet([FromBody] Tweet tweet)
        {
            // Call Sentiment Scorer to analyze the sentiment of the tweet
            var result = await _daprClient.InvokeMethodAsync<Tweet, SentimentScore>(
                HttpMethod.Post,
                "sentimentscorer-api",
                "sentiment",
                tweet);

            _logger.LogInformation($"Analyzed sentiment for tweet {tweet.Id}: Score: {result.Score}");

            // Save Tweet to MongoDB using State API
            //await _daprClient.SaveStateAsync("sentimentscorer", tweet.Id, tweet);
            var state = await _daprClient.GetStateEntryAsync<SentimentScore>("sentiment-scorer", tweet.Id);
            state.Value = result;
            await state.SaveAsync();
            _logger.LogInformation($"Saved tweet with id {tweet.Id}");

            // Publish to Redis Pub/Sub for processed tweets
            await _daprClient.PublishEventAsync("processed-tweets-pubsub", "processed-tweets", result);
            _logger.LogInformation($"Published scored tweet {tweet.Id}");

            return Ok();
        }
    }
}
