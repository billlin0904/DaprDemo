using Microsoft.AspNetCore.Mvc;

namespace SentimentScorer.Controllers
{
    [ApiController]
    [Route("score")]
    public class SentimentScorer : ControllerBase
    {
        private readonly ILogger<SentimentScorer> _logger;

        public SentimentScorer(ILogger<SentimentScorer> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ScoreTweet([FromBody] Tweet tweet)
        {
            // Simple sentiment scoring logic for demonstration purposes
            var score = new SentimentScore
            {
                TweetId = tweet.Id,
                Score = tweet.Content.Contains("good") ? 1.0 : 0.0
            };

            _logger.LogInformation($"Scored tweet with id {tweet.Id}, Score: {score.Score}");
            return Ok(score);
        }
    }
}
