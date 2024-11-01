﻿using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace TweetViewer.Controllers
{
    [ApiController]
    public class TweetViewerController : ControllerBase
    {
        private readonly ILogger<TweetViewerController> _logger;
        private readonly WebSocketHandler _webSocketHandler;

        public TweetViewerController(ILogger<TweetViewerController> logger, WebSocketHandler webSocketHandler)
        {
            _logger = logger;
            _webSocketHandler = webSocketHandler;
        }

        [HttpPost("/tweets/processed")]
        [Topic("processed-tweets-pubsub", "processed-tweets")]
        public async Task<IActionResult> DisplayProcessedTweet([FromBody] SentimentScore score)
        {
            _logger.LogInformation($"Displaying tweet with id {score.TweetId}, Sentiment Score: {score.Score}");
            // 通過 WebSocket 發送消息到前端
            await _webSocketHandler.SendMessageAsync(score);
            return Ok();
        }
    }

}
