using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using StackExchange.Redis;
using TweetViewer.Services;

namespace TweetViewer
{
    public class WebSocketHub
    {
        private readonly Dictionary<string, Dictionary<string, List<WebSocket>>> _subscriptions = new Dictionary<string, Dictionary<string, List<WebSocket>>>();
        private readonly List<WebSocket> _sockets = new List<WebSocket>();
        private readonly PlayerSettingsService _playerSettingsService;

        public WebSocketHub(PlayerSettingsService playerSettingsService)
        {
            _playerSettingsService = playerSettingsService;
        }

        public async Task Handle(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                _sockets.Add(socket);

                await Receive(socket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleReceivedPacket(message, socket);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _sockets.Remove(socket);
                        RemoveSocketFromSubscriptions(socket);
                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                });
            }
        }

        public async Task SendMessageAsync(SentimentScore score)
        {
            var message = JsonSerializer.Serialize(score);
            var buffer = ArrayPool<byte>.Shared.Rent(System.Text.Encoding.UTF8.GetByteCount(message));
            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(message, buffer);
                var segment = new ArraySegment<byte>(buffer, 0, bytesWritten);

                foreach (var socket in _sockets)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    handleMessage(result, buffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task SendMessageAsync<TPacket>(Packet<TPacket> packet)
        {
            var message = JsonSerializer.Serialize(packet);
            int bufferSize = Encoding.UTF8.GetByteCount(message);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                int bytesWritten = Encoding.UTF8.GetBytes(message, buffer);
                var segment = new ArraySegment<byte>(buffer, 0, bytesWritten);

                foreach (var socket in _sockets)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task HandleReceivedPacket(string message, WebSocket socket)
        {
            try
            {
                var basePacket = Packet<object>.Parse(message);

                if (basePacket == null || basePacket.CommandId == 0)
                {
                    await SendMessageAsync(new Packet<object>
                    {
                        CommandId = 0,
                        Details = new { error = "Invalid packet format" }
                    });
                    return;
                }

                switch (basePacket.CommandId)
                {
                    case 102:
                        var subscribePacket = Packet<SubscribePacketDetails>.Parse(message);
                        if (subscribePacket != null)
                        {
                            await HandleSubscribePacket(subscribePacket, socket);
                        }
                        break;

                    case 211:
                        var winScorePacket = Packet<WinScorePacketDetails>.Parse(message);
                        if (winScorePacket != null)
                        {
                            await HandleWinScorePacket(winScorePacket);
                        }
                        break;

                    default:
                        await SendMessageAsync(new Packet<object>
                        {
                            CommandId = 0,
                            Details = new { error = "Unsupported CommandId" }
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                await SendMessageAsync(new Packet<object>
                {
                    CommandId = 0,
                    Details = new { error = $"Error processing packet: {ex.Message}" }
                });
            }
        }

        private async Task HandleSubscribePacket(Packet<SubscribePacketDetails> subscribePacket, WebSocket socket)
        {
            var playerAccount = subscribePacket.Details.PlayerAccount;
            var gameId = subscribePacket.Details.GameId;

            if (!_subscriptions.ContainsKey(playerAccount))
            {
                _subscriptions[playerAccount] = new Dictionary<string, List<WebSocket>>();
            }

            if (!_subscriptions[playerAccount].ContainsKey(gameId))
            {
                _subscriptions[playerAccount][gameId] = new List<WebSocket>();
            }

            if (_subscriptions[playerAccount][gameId].Any(s => s == socket))
            {
                _subscriptions[playerAccount][gameId].Add(socket);
            }

            var rtp = await _playerSettingsService.GetPlayerRTP(subscribePacket.Details.PlayerAccount, subscribePacket.Details.GameId);
        }

        public async Task SendMessageToGameSubscribersAsync(string gameId, object message)
        {
            var serializedMessage = JsonSerializer.Serialize(message);
            var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(serializedMessage));

            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(serializedMessage, buffer);
                var segment = new ArraySegment<byte>(buffer, 0, bytesWritten);

                foreach (var playerSubscriptions in _subscriptions.Values)
                {
                    if (playerSubscriptions.TryGetValue(gameId, out var sockets))
                    {
                        foreach (var socket in sockets)
                        {
                            if (socket.State == WebSocketState.Open)
                            {
                                await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private Task HandleAdjustRTPPacket(Packet<AdjustRTPPacketDetails> adjustRTPPacket)
        {
            throw new NotImplementedException();
        }

        private Task HandleWinScorePacket(Packet<WinScorePacketDetails> winScorePacket)
        {
            
            throw new NotImplementedException();
        }

        private void RemoveSocketFromSubscriptions(WebSocket socket)
        {
            foreach (var playerSubscriptions in _subscriptions.Values)
            {
                foreach (var gameSubscriptions in playerSubscriptions.Values)
                {
                    if (gameSubscriptions.Contains(socket))
                    {
                        gameSubscriptions.Remove(socket);
                    }
                }
            }
        }
    }
}
