using Othello.Core.Game;
using Othello.Server.Models;
using Othello.Shared;
using Othello.AI;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Drawing;

using Point = Othello.Core.Game.Point;

#region DI登録と起動処理
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});
builder.Services.AddSignalR();
var app = builder.Build();
var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
#endregion

#region モデル・状態の初期化
var sessions = new ConcurrentDictionary<string, GameSession>();
var waitingPlayers = new Queue<(string MatchId, Session Session)>();
#endregion

#region エンドポイント: /join

string GenerateRandomMatchId()
{
    var random = new Random();
    string matchId;
    do
    {
        matchId = random.Next(1000, 10000).ToString();
    } while (sessions.ContainsKey(matchId));
    return matchId;
}

IResult HandleJoinWithMatchId(JoinRequest request)
{
    var matchId = request.MatchId!;
    var game = sessions.GetOrAdd(matchId, _ => new GameSession());

    if (!request.IsObserver && game.Players.Count >= 2)
    {
        Console.WriteLine($"[JOIN] 拒否: {request.Name} → matchId={matchId}（満員）");
        return Results.BadRequest(new { Error = "この部屋は満員です。" });
    }

    var sessionId = Guid.NewGuid();
    Stone color = request.IsObserver ? Stone.Empty : (game.Players.Count == 0 ? Stone.Black : Stone.White);
    var session = new Session(sessionId, request.Name, color);

    if (request.IsObserver)
    {
        game.Observers.Add(session);
        Console.WriteLine($"[OBSERVE] {request.Name} が観戦: matchId={matchId}");
    }
    else
    {
        game.Players.Add(session);
        Console.WriteLine($"[JOIN] {request.Name} が参加: matchId={matchId}, 色={color}");
    }

    return Results.Ok(new JoinResponse(sessionId, matchId, color, request.IsObserver));
}

app.MapPost("/join", async (HttpRequest req, JoinRequest request) =>
{
    var connectionId = req.HttpContext.Request.Headers["X-ConnectionId"].FirstOrDefault();
    if (string.IsNullOrEmpty(connectionId))
    {
        return Results.BadRequest(new { Error = "接続IDが必要です。" });
    }

    if (request.VsAI)
    {
        var matchId = GenerateRandomMatchId();
        var sessionId = Guid.NewGuid();
        var player = new Session(sessionId, request.Name, Stone.Black)
        {
            ConnectionId = connectionId
        };

        var ai = new AlphaBetaAI(request.AiLevel);
        var aiSession = new Session(Guid.NewGuid(), "Computer", Stone.White);

        var game = new GameSession { AIPlayer = ai };
        game.Players.Add(player);
        game.Players.Add(aiSession);

        sessions[matchId] = game;

        Console.WriteLine($"[JOIN] {request.Name} vs AI: matchId={matchId}, Depth={request.AiLevel}");

        // 即開始
        await hubContext.Clients.Client(connectionId).SendAsync("MatchFound", new
        {
            MatchId = matchId,
            SessionId = sessionId,
            AssignedColor = (int)Stone.Black
        });

        return Results.Accepted();
    }

    if (!string.IsNullOrWhiteSpace(request.MatchId))
    {
        return HandleJoinWithMatchId(request); // マニュアルID対応は別途処理
    }

    if (request.IsObserver)
    {
        return Results.BadRequest(new { Error = "観戦はマッチIDの指定が必要です。" });
    }

    // マッチング処理
    if (waitingPlayers.TryDequeue(out var opponent))
    {
        var matchId = opponent.MatchId;
        var game = sessions.GetOrAdd(matchId, _ => new GameSession());

        var sessionId = Guid.NewGuid();
        var session = new Session(sessionId, request.Name, Stone.White)
        {
            ConnectionId = connectionId
        };

        game.Players.Add(session);

        Console.WriteLine($"[MATCHED] {request.Name} が待機者とマッチ: matchId={matchId}");

        // 双方に通知
        await hubContext.Clients.Client(opponent.Session.ConnectionId!).SendAsync("MatchFound", new
        {
            MatchId = matchId,
            SessionId = opponent.Session.Id,
            AssignedColor = (int)opponent.Session.Color
        });
        Console.WriteLine($"[SIGNALR] MatchFound sent to {session.Name} ({session.ConnectionId})");


        await hubContext.Clients.Client(session.ConnectionId!).SendAsync("MatchFound", new
        {
            MatchId = matchId,
            SessionId = session.Id,
            AssignedColor = (int)session.Color
        });

        Console.WriteLine($"[SIGNALR] MatchFound sent to {session.Name} ({session.ConnectionId})");

        return Results.Accepted();
    }
    else
    {
        var matchId = GenerateRandomMatchId();
        var sessionId = Guid.NewGuid();
        var session = new Session(sessionId, request.Name, Stone.Black)
        {
            ConnectionId = connectionId
        };

        waitingPlayers.Enqueue((matchId, session));

        var game = sessions.GetOrAdd(matchId, _ => new GameSession());
        game.Players.Add(session);

        Console.WriteLine($"[WAIT] {request.Name} が待機中: matchId={matchId}");

        return Results.Accepted(); // まだ対戦相手がいないので通知はしない
    }
});

#endregion

#region エンドポイント: /state
app.MapGet("/state", (Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
        return Results.NotFound();

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId)
               ?? game.Observers.FirstOrDefault(p => p.Id == sessionId);

    if (session is null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        Board = game.State.Board.Snapshot.Select(row => row.Select(s => (int)s).ToArray()).ToArray(),
        Turn = game.State.Turn,
        LegalMoves = game.State.GetLegalMoves(),
        IsFinished = game.State.Turn == Stone.Empty,
        Winner = game.State.IsFinished(out var winner) ? winner : null,
        SessionId = sessionId,
        MatchId = matchId,
        YourColor = session.Color,
        BlackPlayerName = game.Players.FirstOrDefault(p => p.Color == Stone.Black)?.Name,
        WhitePlayerName = game.Players.FirstOrDefault(p => p.Color == Stone.White)?.Name
    });
});
#endregion

#region エンドポイント: /move
app.MapPost("/move", async (HttpRequest req, Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
        return Results.BadRequest();

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId);
    if (session is null) return Results.Unauthorized();
    if (game.State.Turn != session.Color) return Results.BadRequest(new { Error = "手番ではありません。" });

    MoveRequest? moveDto = await req.ReadFromJsonAsync<MoveRequest>();
    if (moveDto is null)
        return Results.BadRequest(new { Error = "リクエストボディが不正です。" });

    var move = new Point(moveDto.Value.Row, moveDto.Value.Col);

    if (!game.State.TryMove(move, out var flipped))
        return Results.BadRequest(new { Error = "不正な手です。" });

    await hubContext.Clients.Group(matchId).SendAsync("Update", new
    {
        Move = new { move.Row, move.Col },
        Flipped = flipped.Select(p => new { p.Row, p.Col }).ToList()
    });

    // AIの手番が続く限り自動処理
    while (game.AIPlayer != null && game.State.Turn == Stone.White)
    {
        await Task.Delay(1000); // ⏱️ 1秒待機してから実行

        var legalMoves = game.State.GetLegalMoves();
        if (legalMoves.Count == 0)
        {
            game.State.TryMove(new Point(-1, -1), out _);
            continue;
        }

        var aiMove = game.AIPlayer.SelectMove(game.State.Board, Stone.White, legalMoves);
        if (game.State.TryMove(aiMove, out var aiFlipped))
        {
            await hubContext.Clients.Group(matchId).SendAsync("Update", new
            {
                Move = new { aiMove.Row, aiMove.Col },
                Flipped = aiFlipped.Select(p => new { p.Row, p.Col }).ToList()
            });
        }
        else break;
    }


    // 対戦が終了したら通知してから削除を遅延実行
    if (game.State.IsFinished(out var winner))
    {
        if (winner is not null)
        {
            await hubContext.Clients.Group(matchId).SendAsync("GameOver", new
            {
                Winner = (int)winner,
                MatchId = matchId
            });
        }

        // セッション削除は3秒後に実施（UIの描画・表示の余裕を持たせる）
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            if (sessions.TryRemove(matchId, out _))
            {
                Console.WriteLine($"[CLEANUP] 対戦終了によりセッション削除: matchId={matchId}");
            }
        });
    }

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

#region SignalRハブ登録
app.MapHub<GameHub>("/gamehub");
#endregion

app.Run();
