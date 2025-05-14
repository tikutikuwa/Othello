using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Othello.Core.Game;

using Point = Othello.Core.Game.Point;

namespace Othello.Client.Wpf
{
    public class PointDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsValid { get; set; }
    }

    public class GameStateDto
    {
        public int[][] Board { get; set; } = [];
        public Stone Turn { get; set; }
        public List<PointDto> LegalMoves { get; set; } = [];
        public bool IsFinished { get; set; }
        public Stone? Winner { get; set; }
    }

    public class MoveResultDto
    {
        public bool Success { get; set; }
        public PointDto Move { get; set; } = new();
        public List<PointDto> Flipped { get; set; } = [];
    }

    public class OthelloApiClient
    {
        private readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        public async Task<GameStateDto> GetStateAsync()
        {
            return await _http.GetFromJsonAsync<GameStateDto>("http://localhost:5000/state")
                ?? throw new Exception("状態の取得に失敗しました");
        }

        public async Task<MoveResultDto?> PostMoveAsync(Point p)
        {
            var res = await _http.PostAsJsonAsync("http://localhost:5000/move", p);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<MoveResultDto>();
        }
    }

    public partial class GameWindow : Window
    {
        private readonly Border[,] cells = new Border[8, 8];
        private readonly OthelloApiClient api = new();

        public GameWindow()
        {
            InitializeComponent();
            BuildBoard();
            _ = RedrawFromServerAsync();
        }

        private void BuildBoard()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var rect = new Border
                    {
                        Background = Brushes.ForestGreen,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Tag = new Point(r, c)
                    };
                    rect.MouseLeftButtonUp += OnCellClickAsync;
                    cells[r, c] = rect;
                    BoardGrid.Children.Add(rect);
                }
        }

        private async void OnCellClickAsync(object sender, RoutedEventArgs e)
        {
            if (sender is not Border b || b.Tag is not Point p) return;
            var result = await api.PostMoveAsync(p);
            if (result?.Success == true)
            {
                await RedrawFromServerAsync(result.Move, result.Flipped);
            }
            else
            {
                MessageBox.Show("非合法な手、または通信エラーです。", "着手エラー");
            }
        }

        private async Task RedrawFromServerAsync(PointDto? move = null, List<PointDto>? flipped = null)
        {
            StatusText.Text = "サーバーに接続中...";

            try
            {
                var state = await api.GetStateAsync();
                StatusText.Text = "";
                DrawBoard(state, move, flipped);
            }
            catch (Exception ex)
            {
                StatusText.Text = "接続できませんでした";
                MessageBox.Show("サーバーに接続できませんでした。\n" + ex.Message, "通信エラー");
                Close();
            }
        }

        private void DrawBoard(GameStateDto state, PointDto? move = null, List<PointDto>? flipped = null)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var border = cells[r, c];
                    border.Child = null;

                    var stone = (Stone)state.Board[r][c];
                    if (stone != Stone.Empty)
                    {
                        var isFlipped = flipped?.Any(p => p.Row == r && p.Col == c) == true;
                        var isMove = move?.Row == r && move?.Col == c;
                        if (isFlipped && !isMove)
                        {
                            border.Child = AnimateFlip(stone);
                        }
                        else
                        {
                            border.Child = CreateStone(stone);
                        }
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

            Title = state.IsFinished
                ? state.Winner switch
                {
                    Stone.Black => "ゲーム終了！勝者: ● 黒",
                    Stone.White => "ゲーム終了！勝者: ○ 白",
                    null => "ゲーム終了！引き分け",
                    _ => "ゲーム終了"
                }
                : $"Othello - {state.Turn} の手番";
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
                Margin = new Thickness(4),
                //Effect = new System.Windows.Media.Effects.DropShadowEffect
                //{
                //    Color = Colors.Black,
                //    ShadowDepth = 2,
                //    BlurRadius = 4,
                //    Opacity = 0.4,
                //    Direction = 315 // 光が左上から来るように設定
                //}

            };
        }

        private Grid AnimateFlip(Stone toStone)
        {
            var stone = CreateStone(toStone);

            // 固定方向の影を持つ石背景（非反転）
            var shadow = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.Transparent,
                Margin = new Thickness(4),
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

            return new Grid
            {
                Children = { shadow, stone }
            };
        }

    }
}