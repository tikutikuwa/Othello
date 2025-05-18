namespace Othello.Shared;

/// <summary>
/// クライアントからの着手リクエスト
/// 盤面の座標を指定する
/// </summary>
/// <param name="Row">行（0～7）</param>
/// <param name="Col">列（0～7）</param>
public readonly record struct MoveRequest(int Row, int Col);
