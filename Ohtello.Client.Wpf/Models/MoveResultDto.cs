using System.Collections.Generic;

namespace Othello.Client.Wpf.Models;

/// <summary>
/// サーバーから返される着手結果
/// </summary>
public class MoveResultDto
{
    public bool Success { get; set; }
    public PointDto Move { get; set; } = new();
    public List<PointDto> Flipped { get; set; } = [];
}
