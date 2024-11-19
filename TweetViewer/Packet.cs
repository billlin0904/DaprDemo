namespace TweetViewer
{
    // 泛型封包類別
    public class Packet<TPacket>
    {
        public int CommandId { get; set; }
        public TPacket Details { get; set; }

        public static Packet<TPacket>? Parse(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Packet<TPacket>>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    public class SubscribePacketDetails
    {
        public string PlayerAccount { get; set; }
        public string GameId { get; set; }
    }

    public class WinScorePacketDetails
    {
        public string PlayerAccount { get; set; }
        public string WinAmount { get; set; }
        public string RoundId { get; set; }
        public string Status { get; set; }
    }

    public class AdjustRTPPacketDetails
    {
        public string GameId { get; set; }
        public int RTPValue { get; set; }
    }

    public class RewardPacketDetails
    {
        public string PlayerAccount { get; set; }
        public string RewardAmount { get; set; }
    }

    public class PenaltyPacketDetails
    {
        public string PlayerAccount { get; set; }
        public string PenaltyAmount { get; set; }
    }
}
