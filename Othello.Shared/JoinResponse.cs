using Othello.Core.Game;

namespace Othello.Shared;

/// <summary>
/// サーバーからの参加応答
/// マッチIDと割り当てられたセッション情報を含む
/// </summary>
/// <param name="SessionId">このクライアントのセッションID</param>
/// <param name="MatchId">参加したマッチID</param>
/// <param name="AssignedColor">割り当てられた石の色。観戦者は Empty</param>
/// <param name="IsObserver">観戦者かどうか</param>
public record JoinResponse(Guid SessionId, string MatchId, Stone AssignedColor, bool IsObserver);

