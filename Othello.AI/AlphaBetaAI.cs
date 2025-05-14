using Othello.Core.Game;

namespace Othello.AI;

public class AlphaBetaAI : IPlayerAI
{
    private readonly int _maxDepth;

    public AlphaBetaAI(int maxDepth = 4)
    {
        _maxDepth = maxDepth;
    }

    public Point SelectMove(Board board, Stone turn, IReadOnlyList<Point> legalMoves)
    {
        if (legalMoves.Count == 0) return new Point(-1, -1);

        Point bestMove = legalMoves[0];
        int bestScore = int.MinValue;

        foreach (var move in legalMoves)
        {
            var clonedGame = CloneGameState(board, turn);
            clonedGame.TryMove(move, out _);
            int score = AlphaBeta(clonedGame, _maxDepth - 1, int.MinValue, int.MaxValue, turn);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int AlphaBeta(GameState state, int depth, int alpha, int beta, Stone maximizingPlayer)
    {
        if (depth == 0 || state.IsFinished(out _))
        {
            return Evaluate(state, maximizingPlayer);
        }

        var legalMoves = state.GetLegalMoves();
        if (legalMoves.Count == 0)
        {
            // Pass turn
            state.TryMove(new Point(-1, -1), out _);
            return AlphaBeta(state, depth - 1, alpha, beta, maximizingPlayer);
        }

        if (state.Turn == maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in legalMoves)
            {
                var next = CloneGameState(state.Board, state.Turn);
                next.TryMove(move, out _);
                int eval = AlphaBeta(next, depth - 1, alpha, beta, maximizingPlayer);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in legalMoves)
            {
                var next = CloneGameState(state.Board, state.Turn);
                next.TryMove(move, out _);
                int eval = AlphaBeta(next, depth - 1, alpha, beta, maximizingPlayer);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    private int Evaluate(GameState state, Stone player)
    {
        int score = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                if (state.Board[r, c] == player) score++;
                else if (state.Board[r, c] == player.Opponent()) score--;
            }
        return score;
    }

    private GameState CloneGameState(Board originalBoard, Stone turn)
    {
        var clone = new GameState();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                clone.Board.Set(new Point(r, c), originalBoard[r, c]);
        typeof(GameState).GetProperty("Turn")?.SetValue(clone, turn); // Needs a workaround if Turn is private setter
        return clone;
    }
}
