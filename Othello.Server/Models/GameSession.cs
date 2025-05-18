using Othello.Core.Game;

namespace Othello.Server.Models;

public class GameSession
{
    public GameState State { get; } = new();
    public List<Session> Players { get; } = new();
}
