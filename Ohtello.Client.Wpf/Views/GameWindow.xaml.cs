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
using Othello.Client.Wpf.Views.Components;
using System;
#endregion

namespace Othello.Client.Wpf.Views
{

    public partial class GameWindow : Window
    {
        #region フィールド

        private readonly Guid sessionId;
        private readonly string matchId;
        private readonly OthelloApiClient api = new();
        private readonly Border[,] cells = new Border[8, 8];
        private readonly bool isObserver;
        private HubConnection? hub;

        private List<PointDto> currentLegalMoves = new();
        private readonly BoardRenderer boardRenderer;
        private int[][]? previousBoard = null;
        private Stone? previousTurn = null;

        #endregion


        #region 初期化

        public GameWindow(Guid sessionId, string matchId, bool isObserver = false)
        {
            this.sessionId = sessionId;
            this.matchId = matchId;
            this.isObserver = isObserver;

            InitializeComponent();
            BuildBoard();
            boardRenderer = new BoardRenderer(cells);
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
            if (isObserver) return; // 観戦者は打てない

            if (sender is not Border b || b.Tag is not Point p) return;

            if (!currentLegalMoves.Any(m => m.Row == p.Row && m.Col == p.Col))
                return;

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
            boardRenderer.Render(state, move, flipped);
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

        #endregion
    }
}
