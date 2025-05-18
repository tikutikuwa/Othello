using Othello.Core.Game;
using Othello.AI;

namespace Othello.Server.Models
{
    /// <summary>
    /// 1つの対戦マッチに対応するセッション情報を管理するクラス。
    /// 対戦中の状態（盤面）および、参加プレイヤー・観戦者を保持する。
    /// </summary>
    public class GameSession
    {
        /// <summary>
        /// 現在の盤面と手番を管理するゲーム状態。
        /// </summary>
        public GameState State { get; } = new();

        /// <summary>
        /// 対戦プレイヤーのリスト（最大2名）。
        /// </summary>
        public List<Session> Players { get; } = new();

        /// <summary>
        /// 観戦者のリスト（任意人数）。
        /// </summary>
        public List<Session> Observers { get; } = new();

        /// <summary>
        /// AI プレイヤーのインスタンス（AI対戦の場合のみ設定）
        /// </summary>
        public IPlayerAI? AIPlayer { get; set; } 
    }
}
