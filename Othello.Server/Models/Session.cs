using Othello.Core.Game;

namespace Othello.Server.Models;

/// <summary>
/// プレイヤーまたは観戦者のセッション情報を表すレコード。
/// </summary>
/// <param name="Id">このクライアントのセッションID</param>
/// <param name="Name">表示名（プレイヤー名）</param>
/// <param name="Color">割り当てられた石の色。観戦者は Stone.Empty</param>
public record class Session(Guid Id, string Name, Stone Color)
{
    public string? ConnectionId { get; set; }
}
