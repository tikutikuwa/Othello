using Othello.Core.Game;
using Othello.Server.Models;
using Othello.Shared;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// �}�b�`���Ƃ̃Q�[���Z�b�V�����Ǘ�
var sessions = new ConcurrentDictionary<string, GameSession>();

// /join: �v���C���[���Q�[���ɎQ������
app.MapPost("/join", (JoinRequest request) =>
{
    var matchId = request.MatchId ?? Guid.NewGuid().ToString();
    var game = sessions.GetOrAdd(matchId, _ => new GameSession());

    if (game.Players.Count >= 2)
    {
        Console.WriteLine($"[JOIN] �Q������: {request.Name} �� matchId={matchId}�i�����j");
        return Results.BadRequest(new { Error = "���̕����͖����ł��B" });
    }

    var assignedColor = game.Players.Count == 0 ? Stone.Black : Stone.White;
    var session = new Session(Guid.NewGuid(), request.Name, assignedColor);
    game.Players.Add(session);

    Console.WriteLine($"[JOIN] {request.Name} ���Q��: matchId={matchId}, sessionId={session.Id}, �F={assignedColor}");

    return Results.Ok(new JoinResponse(session.Id, matchId, assignedColor));
});


// /state: ���݂̔Ֆʏ�Ԃ��擾
app.MapGet("/state", (Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
    {
        Console.WriteLine($"[STATE] �Y���}�b�`�Ȃ�: matchId={matchId}");
        return Results.NotFound();
    }

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId);
    if (session is null)
    {
        Console.WriteLine($"[STATE] �F�؎��s: sessionId={sessionId}, matchId={matchId}");
        return Results.Unauthorized();
    }

    Console.WriteLine($"[STATE] ����: sessionId={sessionId}, matchId={matchId}, Turn={game.State.Turn}");

    return Results.Ok(new
    {
        Board = game.State.Board.Snapshot.Select(row => row.Select(s => (int)s).ToArray()).ToArray(),
        Turn = game.State.Turn,
        LegalMoves = game.State.GetLegalMoves(),
        IsFinished = game.State.Turn == Stone.Empty,
        Winner = game.State.IsFinished(out var winner) ? winner : null,
        SessionId = sessionId,
        MatchId = matchId,
        YourColor = session.Color
    });
});


// /move: �v���C���[�̒��菈��
app.MapPost("/move", async (HttpRequest req, Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
        return Results.BadRequest();

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId);
    if (session is null) return Results.Unauthorized();
    if (game.State.Turn != session.Color) return Results.BadRequest(new { Error = "��Ԃł͂���܂���B" });

    MoveRequest? moveDto = await req.ReadFromJsonAsync<MoveRequest>();
    if (moveDto is null)
    {
        Console.WriteLine("[MOVE] �����ȃ��N�G�X�g�{�f�B�inull�j");
        return Results.BadRequest(new { Error = "���N�G�X�g�{�f�B���s���ł��B" });
    }

    var move = new Point(moveDto.Value.Row, moveDto.Value.Col);

    if (!game.State.TryMove(move, out var flipped))
        return Results.BadRequest(new { Error = "�s���Ȏ�ł��B" });

    Console.WriteLine($"[MOVE] ����: {move.Row},{move.Col} by {session.Color}");

    return Results.Ok(new
    {
        Success = true,
        Move = moveDto,
        Flipped = flipped,
        SessionId = sessionId,
        MatchId = matchId
    });
});


app.Run();
