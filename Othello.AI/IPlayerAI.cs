// in Othello.AI project
using Othello.Core.Game;

namespace Othello.AI;

public interface IPlayerAI
{
    Point SelectMove(Board board, Stone turn, IReadOnlyList<Point> legalMoves);
}
