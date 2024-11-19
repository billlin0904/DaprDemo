using StackExchange.Redis;

namespace TweetViewer.Services
{
    public class PlayerSettingsService
    {
        private readonly IConnectionMultiplexer _redis;

        public PlayerSettingsService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task SetPlayerRTP(string playerAccount, string gameId, int rtpValue)
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync("Player_RTP_Settings", $"{playerAccount}:{gameId}", rtpValue);
        }

        public async Task<int?> GetPlayerRTP(string playerAccount, string gameId)
        {
            var db = _redis.GetDatabase();
            var value = await db.HashGetAsync("Player_RTP_Settings", $"{playerAccount}:{gameId}");
            return value.HasValue ? (int?)int.Parse(value) : null;
        }

        public async Task SetPlayerRewardSettings(string playerAccount, string gameId, string rewardSettingsJson)
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync("Player_Reward_Settings", $"{playerAccount}:{gameId}", rewardSettingsJson);
        }

        public async Task<string> GetPlayerRewardSettings(string playerAccount, string gameId)
        {
            var db = _redis.GetDatabase();
            var value = await db.HashGetAsync("Player_Reward_Settings", $"{playerAccount}:{gameId}");
            return value.HasValue ? value.ToString() : string.Empty;
        }
    }
}
