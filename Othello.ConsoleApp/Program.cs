using Othello.Core.Game;
using Othello.AI;
using System.Reflection;

IPlayerAI blackAI = new AlphaBetaAI(5);
IPlayerAI whiteAI = new RandomAI();
var game = new GameState();

Stone? winner;
while (!game.IsFinished(out winner))
{
    Console.Clear();
    PrintBoard(game.Board);
    Console.WriteLine($"Turn: {game.Turn}");

    var legalMoves = game.GetLegalMoves();
    if (legalMoves.Count == 0)
    {
        Console.WriteLine("No legal moves. Passing turn.");
        game.TryMove(new Point(-1, -1), out _);
        Console.ReadKey();
        continue;
    }

    IPlayerAI currentAI = game.Turn == Stone.Black ? blackAI : whiteAI;
    var move = currentAI.SelectMove(game.Board, game.Turn, legalMoves);
    Console.WriteLine($"AI chose move: ({move.Row}, {move.Col})");

    game.TryMove(move, out _);
    Console.ReadKey();
}

Console.Clear();
PrintBoard(game.Board);
Console.WriteLine($"Game over! Winner: {winner}");


// --------- Board Printer ---------
static void PrintBoard(Board board)
{
    Console.WriteLine("  0 1 2 3 4 5 6 7");
    for (int r = 0; r < 8; r++)
    {
        Console.Write(r + " ");
        for (int c = 0; c < 8; c++)
        {
            var stone = board[r, c];
            Console.Write(stone switch
            {
                Stone.Black => "● ",
                Stone.White => "○ ",
                _ => ". "
            });
        }
        Console.WriteLine();
    }
}
