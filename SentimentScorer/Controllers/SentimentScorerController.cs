using Microsoft.AspNetCore.Mvc;

namespace SentimentScorer.Controllers
{
    [ApiController]
    public class SentimentScorerController : ControllerBase
    {
        private readonly ILogger<SentimentScorerController> _logger;

        public SentimentScorerController(ILogger<SentimentScorerController> logger)
        {
            _logger = logger;
        }

        [HttpPost("sentiment")]
        public IActionResult AnalyzeSentiment([FromBody] Tweet tweet)
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
