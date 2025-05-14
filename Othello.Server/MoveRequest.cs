namespace Othello.Server.Models;

/// <summary>
/// クライアントからの着手リクエストを受け取る構造体
/// </summary>
public readonly record struct MoveRequest(int Row, int Col);
