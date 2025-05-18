using Othello.Core.Game;
using Othello.Server.Models;
using Othello.Shared;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;


#region DI�o�^�ƋN������

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();

#endregion


#region ���f���E��Ԃ̏�����

// �}�b�`���Ƃ̃Q�[���Z�b�V�����Ǘ�
var sessions = new ConcurrentDictionary<string, GameSession>();

#endregion


#region �G���h�|�C���g: /join

// �v���C���[ or �ϐ�҂��Q�[���ɎQ��
app.MapPost("/join", (JoinRequest request) =>
{
    var matchId = request.MatchId ?? Guid.NewGuid().ToString();
    var game = sessions.GetOrAdd(matchId, _ => new GameSession());

    if (!request.IsObserver && game.Players.Count >= 2)
    {
        Console.WriteLine($"[JOIN] �Q������: {request.Name} �� matchId={matchId}�i�����j");
        return Results.BadRequest(new { Error = "���̕����͖����ł��B" });
    }

    var sessionId = Guid.NewGuid();
    Stone color = Stone.Empty;

    if (request.IsObserver)
    {
        game.Observers.Add(new Session(sessionId, request.Name, Stone.Empty));
        Console.WriteLine($"[OBSERVE] {request.Name} ���ϐ�: matchId={matchId}, sessionId={sessionId}");
    }
    else
    {
        color = game.Players.Count == 0 ? Stone.Black : Stone.White;
        game.Players.Add(new Session(sessionId, request.Name, color));
        Console.WriteLine($"[JOIN] {request.Name} ���Q��: matchId={matchId}, sessionId={sessionId}, �F={color}");
    }

    return Results.Ok(new JoinResponse(sessionId, matchId, color));
});

# endregion


#region �G���h�|�C���g: /state

// �N���C�A���g���猻�݂̔Ֆʏ�Ԃ��擾
app.MapGet("/state", (Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
    {
        Console.WriteLine($"[STATE] �Y���}�b�`�Ȃ�: matchId={matchId}");
        return Results.NotFound();
    }

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId)
               ?? game.Observers.FirstOrDefault(p => p.Id == sessionId);

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

#endregion


#region �G���h�|�C���g: /move

// �v���C���[�̒��菈��
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

    // SignalR �œ��� matchId �̃N���C�A���g�ɒʒm
    await hubContext.Clients.Group(matchId).SendAsync("Update");

    return Results.Ok(new
    {
        Success = true,
        Move = moveDto,
        Flipped = flipped,
        SessionId = sessionId,
        MatchId = matchId
    });
});

#endregion


#region SignalR�n�u�o�^

app.MapHub<GameHub>("/gamehub");

#endregion


app.Run();
