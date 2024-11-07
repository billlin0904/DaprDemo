using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace TweetProvider.Controllers
{
    [ApiController]
    public class TweetProviderController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<TweetProviderController> _logger;

        public TweetProviderController(DaprClient daprClient, ILogger<TweetProviderController> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        [HttpPost("/tweets")]
        public async Task<IActionResult> ProcessTweet(Tweet tweet)
        {
            var state = await _daprClient.GetStateEntryAsync<Tweet>("tweet-store", tweet.Id);
            state.Value = tweet;
            await state.SaveAsync();

            //await _daprClient.SaveStateAsync("tweet-store", tweet.Id, tweet);
            _logger.LogInformation($"Saved tweet with id {tweet.Id}");

            await _daprClient.PublishEventAsync("tweets-pubsub", "tweets", tweet);
            _logger.LogInformation($"Published scored tweet {tweet.Id}");

            return Ok();
        }
    }
}
