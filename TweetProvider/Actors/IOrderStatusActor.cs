using Dapr.Actors;

namespace TweetProvider.Actors
{
    public interface IOrderStatusActor : IActor
    {
        Task<string> Process(string orderId);
        Task<string> GetStatus(string orderId);

        Task StartTimerAsync(string name, string text, TimeSpan dueTime);
        Task StopTimerAsync(string name);
    }
}
