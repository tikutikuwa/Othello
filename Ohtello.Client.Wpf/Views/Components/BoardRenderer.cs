using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Othello.Client.Wpf.Models;
using Othello.Core.Game;

namespace Othello.Client.Wpf.Views.Components;

public class BoardRenderer
{
    private readonly Border[,] cells;

    public BoardRenderer(Border[,] cells)
    {
        this.cells = cells;
    }

    public async Task RenderAsync(GameStateDto state, PointDto? move = null, List<PointDto>? flipped = null)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                cells[r, c].Child = null;
            }
        }

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var stone = (Stone)state.Board[r][c];
                var border = cells[r, c];

                if (stone != Stone.Empty)
                {
                    border.Child = CreateStone(stone);
                }
                else if (state.LegalMoves.Any(p => p.Row == r && p.Col == c))
                {
                    border.Child = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.LightGray,
                        Opacity = 0.6,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
        }

        await Task.CompletedTask; // 非同期互換のため
    }

    private Ellipse CreateStone(Stone stone)
    {
        var gradient = new RadialGradientBrush
        {
            GradientOrigin = new System.Windows.Point(0.3, 0.3),
            Center = new System.Windows.Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };

        if (stone == Stone.Black)
        {
            gradient.GradientStops.Add(new GradientStop(Colors.Gray, 0.0));
            gradient.GradientStops.Add(new GradientStop(Colors.Black, 1.0));
        }
        else
        {
            gradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            gradient.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));
        }

        return new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = gradient,
            Stroke = Brushes.Black,
            StrokeThickness = 1.5,
            Margin = new Thickness(4)
        };
    }
}
