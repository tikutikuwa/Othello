using Othello.Core.Game;
using System.Collections.Generic;

namespace Othello.Client.Wpf.Models;

/// <summary>
/// サーバーから送られてくるゲームの状態
/// </summary>
public class GameStateDto
{
    public int[][] Board { get; set; } = [];
    public Stone Turn { get; set; }
    public List<PointDto> LegalMoves { get; set; } = [];
    public bool IsFinished { get; set; }
    public Stone? Winner { get; set; }
}
