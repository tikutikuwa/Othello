namespace Othello.Core.Game;

/// <summary>
/// オセロのゲーム全体の状態（盤面、手番、ルール処理）を管理するクラス
/// </summary>
public class GameState
{
    /// <summary>
    /// 現在の盤面状態
    /// </summary>
    public Board Board { get; } = new();

    /// <summary>
    /// 現在の手番
    /// </summary>
    public Stone Turn { get; private set; } = Stone.Black;

    /// <summary>
    /// グリッド上での8方向
    /// </summary>
    private static readonly (int dr, int dc)[] Directions =
    [
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),          (0, 1),
        (1, -1),  (1, 0), (1, 1)
    ];

    /// <summary>
    /// 合法手か判定する
    /// </summary>
    /// <param name="p">確認する座標</param>
    /// <returns>合法手であれば true</returns>
    public bool IsLegal(Point p)
    {
        if (!p.IsValid || Board[p] != Stone.Empty) return false;

        foreach (var (dr, dc) in Directions)
        {
            if (HasLine(p, dr, dc)) return true;
        }
        return false;
    }

    /// <summary>
    /// 現在の手番におけるすべての合法手を列挙する
    /// </summary>
    /// <returns>合法手のリスト</returns>
    public IReadOnlyList<Point> GetLegalMoves()
    {
        var list = new List<Point>();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var p = new Point(r, c);
                if (IsLegal(p)) list.Add(p);
            }
        return list;
    }

    /// <summary>
    /// 指定された位置に石を置き、裏返し処理を行う
    /// </summary>
    /// <param name="p">着手する座標</param>
    /// <param name="flipped">裏返った座標の一覧（出力）</param>
    /// <returns>成功すれば true、非合法手で失敗すれば false</returns>
    public bool TryMove(Point p, out List<Point> flipped)
    {
        flipped = new();
        if (!IsLegal(p)) return false;

        Board.Set(p, Turn);

        foreach (var (dr, dc) in Directions)
        {
            var flips = Flippable(p, dr, dc).ToList();
            foreach (var q in flips) Board.Set(q, Turn);
            flipped.AddRange(flips);
        }

        Turn = HasAnyMove(Turn.Opponent()) ? Turn.Opponent() :
               HasAnyMove(Turn) ? Turn : Stone.Empty;

        return true;
    }

    /// <summary>
    /// ゲームが終了しているか判定し、終了していれば勝敗を判定する
    /// </summary>
    /// <param name="winner">勝者（黒／白／引き分け）</param>
    /// <returns>ゲームが終了していれば true</returns>
    public bool IsFinished(out Stone? winner)
    {
        if (Turn != Stone.Empty)
        {
            winner = null;
            return false;
        }

        int b = 0, w = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var s = Board[r, c];
                if (s == Stone.Black) b++;
                if (s == Stone.White) w++;
            }

        winner = b > w ? Stone.Black : w > b ? Stone.White : null;
        return true;
    }

    /// <summary>
    /// 指定方向に裏返せるラインがあるかを判定する
    /// </summary>
    private bool HasLine(Point p, int dr, int dc)
    {
        var opp = Turn.Opponent();
        int r = p.Row + dr, c = p.Col + dc;
        bool seenOpponent = false;

        while (r is >= 0 and < 8 && c is >= 0 and < 8)
        {
            var current = Board[r, c];
            if (current == opp) seenOpponent = true;
            else if (current == Turn) return seenOpponent;
            else break;

            r += dr;
            c += dc;
        }

        return false;
    }

    /// <summary>
    /// 指定方向に裏返せる石の座標列を返す
    /// </summary>
    private IEnumerable<Point> Flippable(Point start, int dr, int dc)
    {
        var list = new List<Point>();
        var opp = Turn.Opponent();
        int r = start.Row + dr, c = start.Col + dc;

        while (r is >= 0 and < 8 && c is >= 0 and < 8)
        {
            var current = Board[r, c];
            if (current == opp) list.Add(new Point(r, c));
            else if (current == Turn) return list;
            else break;

            r += dr;
            c += dc;
        }

        return Enumerable.Empty<Point>();
    }

    /// <summary>
    /// 合法手が1つでも存在するかを確認する
    /// </summary>
    private bool HasAnyMove(Stone s)
    {
        var oldTurn = Turn;
        Turn = s;
        bool any = Enumerable.Range(0, 8)
            .SelectMany(r => Enumerable.Range(0, 8).Select(c => new Point(r, c)))
            .Any(IsLegal);
        Turn = oldTurn;
        return any;
    }
}
