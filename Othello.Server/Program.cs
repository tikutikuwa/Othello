using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Othello.Core.Game;
using Othello.Server.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var game = new GameState();

// Œ»Ý‚Ì”Õ–Êó‘Ô‚ðŽæ“¾
app.MapGet("/state", () =>
{
    game.IsFinished(out var winner);

    return Results.Ok(new
    {
        Board = game.Board.Snapshot.Select(row => row.Select(s => (int)s).ToArray()).ToArray(),
        Turn = game.Turn,
        LegalMoves = game.GetLegalMoves(),
        IsFinished = game.Turn == Stone.Empty,
        Winner = winner
    });
});

// ’…Žè‚ðŽŽ‚Ý‚é
app.MapPost("/move", (MoveRequest p) =>
{
    var move = new Point(p.Row, p.Col);
    if (game.TryMove(move, out var flipped))
    {
        Console.WriteLine($"Move accepted: ({p.Row},{p.Col})");
        return Results.Ok(new { Success = true, Move = p, Flipped = flipped });
    }

    Console.WriteLine($"Move rejected: ({p.Row},{p.Col})");
    return Results.BadRequest(new { Success = false });
});

app.Run();