using Othello.Core.Game;
using Othello.Server.Models;
using Othello.Shared;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;


#region DI登録と起動処理

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();

#endregion


#region モデル・状態の初期化

// マッチごとのゲームセッション管理
var sessions = new ConcurrentDictionary<string, GameSession>();

#endregion


#region エンドポイント: /join

// プレイヤー or 観戦者がゲームに参加
app.MapPost("/join", (JoinRequest request) =>
{
    var matchId = request.MatchId ?? Guid.NewGuid().ToString();
    var game = sessions.GetOrAdd(matchId, _ => new GameSession());

    if (!request.IsObserver && game.Players.Count >= 2)
    {
        Console.WriteLine($"[JOIN] 参加拒否: {request.Name} → matchId={matchId}（満員）");
        return Results.BadRequest(new { Error = "この部屋は満員です。" });
    }

    var sessionId = Guid.NewGuid();
    Stone color = Stone.Empty;

    if (request.IsObserver)
    {
        game.Observers.Add(new Session(sessionId, request.Name, Stone.Empty));
        Console.WriteLine($"[OBSERVE] {request.Name} が観戦: matchId={matchId}, sessionId={sessionId}");
    }
    else
    {
        color = game.Players.Count == 0 ? Stone.Black : Stone.White;
        game.Players.Add(new Session(sessionId, request.Name, color));
        Console.WriteLine($"[JOIN] {request.Name} が参加: matchId={matchId}, sessionId={sessionId}, 色={color}");
    }

    return Results.Ok(new JoinResponse(sessionId, matchId, color));
});

# endregion


#region エンドポイント: /state

// クライアントから現在の盤面状態を取得
app.MapGet("/state", (Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
    {
        Console.WriteLine($"[STATE] 該当マッチなし: matchId={matchId}");
        return Results.NotFound();
    }

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId)
               ?? game.Observers.FirstOrDefault(p => p.Id == sessionId);

    if (session is null)
    {
        Console.WriteLine($"[STATE] 認証失敗: sessionId={sessionId}, matchId={matchId}");
        return Results.Unauthorized();
    }

    Console.WriteLine($"[STATE] 成功: sessionId={sessionId}, matchId={matchId}, Turn={game.State.Turn}");

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


#region エンドポイント: /move

// プレイヤーの着手処理
app.MapPost("/move", async (HttpRequest req, Guid sessionId, string matchId) =>
{
    if (!sessions.TryGetValue(matchId, out var game))
        return Results.BadRequest();

    var session = game.Players.FirstOrDefault(p => p.Id == sessionId);
    if (session is null) return Results.Unauthorized();
    if (game.State.Turn != session.Color) return Results.BadRequest(new { Error = "手番ではありません。" });

    MoveRequest? moveDto = await req.ReadFromJsonAsync<MoveRequest>();
    if (moveDto is null)
    {
        Console.WriteLine("[MOVE] 無効なリクエストボディ（null）");
        return Results.BadRequest(new { Error = "リクエストボディが不正です。" });
    }

    var move = new Point(moveDto.Value.Row, moveDto.Value.Col);

    if (!game.State.TryMove(move, out var flipped))
        return Results.BadRequest(new { Error = "不正な手です。" });

    Console.WriteLine($"[MOVE] 成功: {move.Row},{move.Col} by {session.Color}");

    // SignalR で同じ matchId のクライアントに通知
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


#region SignalRハブ登録

app.MapHub<GameHub>("/gamehub");

#endregion


app.Run();
