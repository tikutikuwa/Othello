using Microsoft.AspNetCore.SignalR;

namespace Othello.Server;

public class GameHub : Hub
{
    public async Task JoinMatch(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        Console.WriteLine($"[HUB] {Context.ConnectionId} joined {matchId}");
    }
}
