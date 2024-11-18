using System.Security.Cryptography;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TweetProvider.Actors;

namespace TweetProvider.Controllers
{
    [ApiController]
    public class TweetProviderController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<TweetProviderController> _logger;

        public TweetProviderController(DaprClient daprClient, IActorProxyFactory actorProxyFactory, ILogger<TweetProviderController> logger)
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

            //await _daprClient.PublishEventAsync("tweets-pubsub", "tweets", tweet);
            //_logger.LogInformation($"Published scored tweet {tweet.Id}");

            //tweet.Id = Guid.NewGuid().ToString("N");
            var actorId = new ActorId("process-" + tweet.Id);
            var proxy = ActorProxy.Create<IOrderStatusActor>(actorId, "OrderStatusActor");

            try
            {
                if (await proxy.GetStatus(tweet.Id) == "process")
                {
                    return Conflict();
                }
            }
            catch (ActorMethodInvocationException ex)
            {
            }

            var _ = await proxy.Process(tweet.Id);

            var now = DateTime.UtcNow;
            var targetTime = DateTime.UtcNow.AddSeconds(new Random().Next(3, 6));
            var dueTime = targetTime - now;

            await proxy.StartTimerAsync(tweet.Id, JsonSerializer.Serialize(tweet), dueTime);

            return Ok();
        }

        [HttpPost("/cancel")]
        public async Task<IActionResult> ProcessCancel(string tweetId)
        {
            var actorId = new ActorId("process-" + tweetId);
            var proxy = ActorProxy.Create<IOrderStatusActor>(actorId, "OrderStatusActor");
            await proxy.StopTimerAsync(tweetId);

            return Ok();
        }
    }
}
