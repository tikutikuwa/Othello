namespace Othello.Core.Game;

/// <summary>
/// オセロの石の種類を表す列挙体
/// </summary>
public enum Stone
{
    Empty = 0,
    Black = 1,
    White = 2
}

/// <summary>
/// Stone に関する拡張メソッドを定義する静的クラス
/// </summary>
public static class StoneExtensions
{
    /// <summary>
    /// 現在の石に対応する相手側の石を取得する
    /// </summary>
    /// <param name="s">現在の石</param>
    /// <returns>相手側の石。Empty の場合も Empty</returns>
    public static Stone Opponent(this Stone s) =>
        s switch
        {
            Stone.Black => Stone.White,
            Stone.White => Stone.Black,
            _ => Stone.Empty
        };
}
