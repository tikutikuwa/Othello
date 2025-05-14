namespace Othello.Core.Game;

/// <summary>
/// オセロ盤上の座標を表す構造体（行・列）
/// </summary>
public readonly struct Point
{
    /// <summary>
    /// 行インデックス（0～7）
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// 列インデックス（0～7）
    /// </summary>
    public int Col { get; }

    /// <summary>
    /// 指定した座標が盤面内（8×8）に収まっているかを判定する
    /// </summary>
    public bool IsValid => Row is >= 0 and < 8 && Col is >= 0 and < 8;

    /// <summary>
    /// 指定した行・列で Point を初期化する
    /// </summary>
    /// <param name="row">行インデックス（0〜7）</param>
    /// <param name="col">列インデックス（0〜7）</param>
    public Point(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
