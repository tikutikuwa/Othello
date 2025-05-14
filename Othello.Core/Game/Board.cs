using System.Collections.ObjectModel;

namespace Othello.Core.Game;

/// <summary>
/// オセロの 8×8 の盤面情報を管理するクラス
/// </summary>
public class Board
{
    private readonly Stone[,] _grid = new Stone[8, 8];

    /// <summary>
    /// 初期配置を設定した 8×8 の盤面を生成する。
    /// </summary>
    public Board()
    {
        // 中央4マスの初期配置
        _grid[3, 3] = Stone.White;
        _grid[3, 4] = Stone.Black;
        _grid[4, 3] = Stone.Black;
        _grid[4, 4] = Stone.White;
    }

    /// <summary>
    /// 指定行・列のマスの石を取得するインデクサ
    /// </summary>
    /// <param name="row">行インデックス（0～7）</param>
    /// <param name="col">列インデックス（0～7）</param>
    /// <returns>指定位置にある石</returns>
    public Stone this[int row, int col] => _grid[row, col];

    /// <summary>
    /// Point 構造体で指定したマスの石を取得するインデクサ
    /// </summary>
    /// <param name="p">座標を表す Point 構造体</param>
    /// <returns>指定位置にある石</returns>
    public Stone this[Point p] => _grid[p.Row, p.Col];

    /// <summary>
    /// 現在の盤面状態を 8×8 の読み取り専用リストとして返す
    /// </summary>
    /// <returns>読み取り専用の Stone 配列</returns>
    public ReadOnlyCollection<ReadOnlyCollection<Stone>> Snapshot =>
            Enumerable.Range(0, 8)
               .Select(r =>
                   new ReadOnlyCollection<Stone>(
                       Enumerable.Range(0, 8).Select(c => _grid[r, c]).ToList()
                   )
               ).ToList().AsReadOnly();

    /// <summary>
    /// 指定したマスに石を配置する。
    /// </summary>
    /// <param name="p">配置するマスの座標</param>
    /// <param name="s">配置する石の色</param>
    public void Set(Point p, Stone s) => _grid[p.Row, p.Col] = s;
}
