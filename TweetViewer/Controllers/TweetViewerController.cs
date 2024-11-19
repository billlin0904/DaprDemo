using Dapr;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace TweetViewer.Controllers
{
    [ApiController]
    public class TweetViewerController : ControllerBase
    {
        private readonly ILogger<TweetViewerController> _logger;
        private readonly WebSocketHub _webSocketHub;
        
        public TweetViewerController(WebSocketHub webSocketHub, ILogger<TweetViewerController> logger)
        {
            _webSocketHub = webSocketHub;
            _logger = logger;
        }

        [HttpPost("/tweets/processed")]
        [Topic("processed-tweets-pubsub", "processed-tweets")]
        public async Task<IActionResult> DisplayProcessedTweet([FromBody] SentimentScore score)
        {
            _logger.LogInformation($"Displaying tweet with id {score.TweetId}, Sentiment Score: {score.Score}");
            // 通過 WebSocket 發送消息到前端
            await _webSocketHub.SendMessageAsync(score);
            return Ok();
        }
    }

}
