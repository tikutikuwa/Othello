using Othello.Core.Game;

namespace Othello.AI;

public class RandomAI : IPlayerAI
{
    private readonly Random _rng = new();

    public Point SelectMove(Board board, Stone turn, IReadOnlyList<Point> legalMoves)
    {
        return legalMoves.Count > 0 ? legalMoves[_rng.Next(legalMoves.Count)] : new Point(-1, -1);
    }
}
