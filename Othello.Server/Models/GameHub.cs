using Microsoft.AspNetCore.SignalR;

namespace Othello.Server.Models;

/// <summary>
/// SignalR による対戦マッチ通知のハブ
/// クライアントは特定のマッチIDに対して接続し、サーバーからのブロードキャストを受信する
/// </summary>
public class GameHub : Hub
{
    /// <summary>
    /// クライアントを指定したマッチの通知グループに参加させる
    /// クライアントはこのグループに対して送られた "Update" イベントを受信できる
    /// </summary>
    /// <param name="matchId">通知を受け取りたいマッチID</param>
    public async Task JoinMatch(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        Console.WriteLine($"[HUB] {Context.ConnectionId} joined {matchId}");
    }
}
