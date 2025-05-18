namespace Othello.Client.Wpf.Models;

/// <summary>
/// 座標データ（サーバーとの通信に使用）
/// </summary>
public class PointDto
{
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsValid { get; set; }
}
