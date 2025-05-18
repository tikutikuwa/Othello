#region using
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using Othello.Core.Game;
using Othello.Client.Wpf.Services;
using Othello.Client.Wpf.Models;


using Point = Othello.Core.Game.Point;
#endregion

namespace Othello.Client.Wpf
{

    public partial class GameWindow : Window
    {
        #region フィールド

        private readonly Guid sessionId;
        private readonly string matchId;
        private readonly OthelloApiClient api = new();
        private readonly Border[,] cells = new Border[8, 8];
        private HubConnection? hub;

        private List<PointDto> currentLegalMoves = new();
        private int[][]? previousBoard = null;
        private Stone? previousTurn = null;

        #endregion

        #region 初期化

        public GameWindow(Guid sessionId, string matchId)
        {
            this.sessionId = sessionId;
            this.matchId = matchId;

            InitializeComponent();
            BuildBoard();
            _ = RedrawFromServerAsync();
            _ = InitializeSignalRAsync();
        }

        private async Task InitializeSignalRAsync()
        {
            hub = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/gamehub")
                .WithAutomaticReconnect()
                .Build();

            hub.On("Update", async () =>
            {
                await Dispatcher.InvokeAsync(() => RedrawFromServerAsync());
            });

            await hub.StartAsync();
            await hub.InvokeAsync("JoinMatch", matchId);
        }

        #endregion

        #region ボード構築・クリック処理

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
            if (!currentLegalMoves.Any(m => m.Row == p.Row && m.Col == p.Col)) return;

            var result = await api.PostMoveAsync(p, sessionId, matchId);
            if (result?.Success == true)
            {
                await RedrawFromServerAsync(result.Move, result.Flipped);
            }
            else
            {
                MessageBox.Show("非合法な手、または通信エラーです。", "着手エラー");
            }
        }

        #endregion

        #region 描画更新・状態取得

        private async Task RedrawFromServerAsync(PointDto? move = null, List<PointDto>? flipped = null)
        {
            StatusText.Text = "サーバーに接続中...";

            try
            {
                var state = await api.GetStateAsync(sessionId, matchId);
                currentLegalMoves = state.LegalMoves;
                StatusText.Text = "";

                if (previousBoard == null || previousTurn != state.Turn || !IsBoardEqual(previousBoard, state.Board))
                {
                    DrawBoard(state, move, flipped);
                    previousBoard = CloneBoard(state.Board);
                    previousTurn = state.Turn;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "接続できませんでした";
                MessageBox.Show("サーバーに接続できませんでした。\n" + ex.Message, "通信エラー");
                Close();
            }
        }

        private static bool IsBoardEqual(int[][] a, int[][] b) =>
            Enumerable.Range(0, 8).All(r => Enumerable.Range(0, 8).All(c => a[r][c] == b[r][c]));

        private static int[][] CloneBoard(int[][] source) =>
            source.Select(row => row.ToArray()).ToArray();

        #endregion

        #region 描画ロジック

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
                        border.Child = isFlipped && !isMove ? AnimateFlip(stone) : CreateStone(stone);
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
                Margin = new Thickness(4)
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

            return new Grid { Children = { shadow, stone } };
        }

        #endregion
    }
}
