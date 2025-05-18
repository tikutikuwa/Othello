namespace Othello.Shared;

/// <summary>
/// クライアントからの参加要求を表すデータ
/// 対戦・観戦どちらもこのリクエストで処理する
/// </summary>
/// <param name="Name">プレイヤーの名前</param>
/// <param name="MatchId">参加したいマッチID。null の場合は新規作成する</param>
/// <param name="IsObserver">観戦モードかどうか</param>
/// <param name="VsAI">AIと対戦するかどうか</param>
/// <param name="AiLevel">AIの強さの指定</param>
public record JoinRequest(string Name, string? MatchId, bool IsObserver, bool VsAI = false, int AiLevel = 4);


