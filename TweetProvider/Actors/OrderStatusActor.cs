using System.Text;
using System.Text.Json;
using Dapr.Actors.Runtime;
using Dapr.Client;
using TweetProvider.Controllers;

namespace TweetProvider.Actors
{
    public class OrderStatusActor : Actor, IOrderStatusActor
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<OrderStatusActor> _logger;

        public OrderStatusActor(DaprClient daprClient, ActorHost host, ILogger<OrderStatusActor> logger)
            : base(host)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        public async Task<string> Process(string orderId)
        {
            await StateManager.AddOrUpdateStateAsync(orderId, "init", (key, currentStatus) => "process");
            return orderId;
        }

        public async Task<string> GetStatus(string orderId)
        {
            return await StateManager.GetStateAsync<string>(orderId);
        }

        public Task StartTimerAsync(string name, string text, TimeSpan dueTime)
        {
            return RegisterTimerAsync(
                name,
                nameof(TimerCallbackAsync),
                Encoding.UTF8.GetBytes(text),
                dueTime,
                TimeSpan.FromMilliseconds(-1));
        }

        public Task StopTimerAsync(string name)
        {
            return UnregisterTimerAsync(name);
        }

        public async Task TimerCallbackAsync(byte[] state)
        {
            var text = Encoding.UTF8.GetString(state);
            var tweet = JsonSerializer.Deserialize<Tweet>(text);
            await _daprClient.PublishEventAsync("tweets-pubsub", "tweets", tweet);
            _logger.LogInformation($"Published scored tweet {tweet.Id}");
            await DeleteActorAsync();
        }
    }
}
