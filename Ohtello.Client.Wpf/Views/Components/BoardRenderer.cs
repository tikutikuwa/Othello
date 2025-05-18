using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

    public void Render(GameStateDto state, PointDto? move = null, List<PointDto>? flipped = null)
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var border = cells[r, c];
                border.Child = null;

                var stone = (Stone)state.Board[r][c];
                if (stone != Stone.Empty)
                {
                    bool isFlipped = flipped?.Any(p => p.Row == r && p.Col == c) == true;
                    bool isMove = move?.Row == r && move?.Col == c;
                    border.Child = isFlipped && !isMove
                        ? AnimateFlip(stone)
                        : CreateStone(stone);
                }
                else if (state.LegalMoves.Any(p => p.Row == r && p.Col == c))
                {
                    border.Child = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.Gray,
                        Opacity = 0.6,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
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
            Margin = new System.Windows.Thickness(4)
        };
    }

    private Grid AnimateFlip(Stone toStone)
    {
        var stone = CreateStone(toStone);

        var shadow = new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = Brushes.Transparent,
            Margin = new System.Windows.Thickness(4),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                ShadowDepth = 2,
                BlurRadius = 4,
                Opacity = 0.4,
                Direction = 315
            }
        };

        var scale = new ScaleTransform(1, 1);
        stone.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        stone.RenderTransform = scale;

        var animation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1,
            To = -1,
            Duration = TimeSpan.FromMilliseconds(300),
            AutoReverse = false
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);

        if (stone.Fill is RadialGradientBrush brush)
        {
            brush.GradientOrigin = new System.Windows.Point(1.0 - brush.GradientOrigin.X, brush.GradientOrigin.Y);
        }

        return new Grid { Children = { shadow, stone } };
    }
}
