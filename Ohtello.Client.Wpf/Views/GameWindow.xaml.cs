using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;
using Othello.Core.Game;
using System.Text.Json;
using Othello.Client.Wpf.Services;
using Othello.Client.Wpf.Models;

using Point = Othello.Core.Game.Point;
using Othello.Client.Wpf.Views.Components;

namespace Othello.Client.Wpf.Views
{
    public partial class GameWindow : Window
    {
        #region フィールド

        private readonly Guid sessionId;
        private readonly string matchId;
        private readonly Stone yourColor;
        private readonly OthelloApiClient api;
        private readonly Border[,] cells = new Border[8, 8];
        private readonly bool isObserver;
        private HubConnection? hub;

        private List<PointDto> currentLegalMoves = new();
        private readonly BoardRenderer boardRenderer;
        private int[][]? previousBoard = null;
        private Stone? previousTurn = null;

        private Stone currentTurn = Stone.Empty;

        #endregion

        #region 初期化

        public GameWindow(Guid sessionId, string matchId, Stone yourColor, bool isObserver, OthelloApiClient api)
        {
            this.sessionId = sessionId;
            this.matchId = matchId;
            this.yourColor = yourColor;
            this.isObserver = isObserver;
            this.api = api;

            InitializeComponent();
            BuildBoard();
            boardRenderer = new BoardRenderer(cells);
            _ = RedrawFromServerAsync();
            _ = InitializeSignalRAsync();
        }


        private async Task InitializeSignalRAsync()
        {
            hub = new HubConnectionBuilder()
                .WithUrl($"{api.ServerBaseUrl}/gamehub")
                .WithAutomaticReconnect()
                .Build();

            hub.On<JsonElement>("Update", async payload =>
            {
                PointDto? move = null;
                List<PointDto>? flipped = null;

                if (payload.TryGetProperty("Move", out var moveElem))
                    move = JsonSerializer.Deserialize<PointDto>(moveElem.GetRawText());

                if (payload.TryGetProperty("Flipped", out var flippedElem))
                    flipped = JsonSerializer.Deserialize<List<PointDto>>(flippedElem.GetRawText());

                await Dispatcher.InvokeAsync(() => RedrawFromServerAsync(move, flipped));
            });

            hub.On<JsonElement>("GameOver", async payload =>
            {
                int winnerValue = payload.GetProperty("Winner").GetInt32();
                Stone winner = (Stone)winnerValue;

                string message = winner switch
                {
                    Stone.Black => "🎉 勝者：● 黒！",
                    Stone.White => "🎉 勝者：○ 白！",
                    _ => "🎉 引き分け！"
                };

                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(message, "ゲーム終了", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                });
            });

            await hub.StartAsync();
            await hub.InvokeAsync("JoinMatch", matchId);
        }


        #endregion

        #region ボード構築・クリック処理

        private void BuildBoard()
        {
            for (int r = 0; r < 8; r++)
            {
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
        }

        private async void OnCellClickAsync(object sender, RoutedEventArgs e)
        {
            if (isObserver) return; // 観戦者は打てない
            if (yourColor != currentTurn) return; // 自分の手番じゃない

            if (sender is not Border b || b.Tag is not Point p) return;
            if (!currentLegalMoves.Any(m => m.Row == p.Row && m.Col == p.Col)) return;

            var result = await api.PostMoveAsync(p, sessionId, matchId);
            if (result?.Success == true)
            {
                await RedrawFromServerAsync(result.Move, result.Flipped);
            }
            else
            {
                // MessageBox.Show("非合法な手、または通信エラーです。", "着手エラー");
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
                currentTurn = state.Turn;
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

        private async void DrawBoard(GameStateDto state, PointDto? move = null, List<PointDto>? flipped = null)
        {
            await boardRenderer.RenderAsync(state, move, flipped);

            if (state.IsFinished)
            {
                TurnText.Text = state.Winner switch
                {
                    Stone.Black => $"🎉 勝者：● {state.BlackPlayerName ?? "黒"}！",
                    Stone.White => $"🎉 勝者：○ {state.WhitePlayerName ?? "白"}！",
                    _ => "🎉 引き分け！"
                };

                await Task.Delay(3000);
                var main = new MainWindow();
                main.Show();
                Close();
                return;
            }
            else
            {
                string? name = state.Turn switch
                {
                    Stone.Black => state.BlackPlayerName,
                    Stone.White => state.WhitePlayerName,
                    _ => null
                };

                string symbol = state.Turn switch
                {
                    Stone.Black => "●",
                    Stone.White => "○",
                    _ => ""
                };

                string suffix = "";
                if (!isObserver)
                {
                    if (yourColor == state.Turn)
                        suffix = "（あなた）";
                    else
                        suffix = "（相手）";
                }

                TurnText.Text = isObserver
                    ? $"{symbol} {name} さんの番です"
                    : $"{symbol} {name}{suffix} さんの番です";
            }


            // スコア集計・表示
            int black = 0, white = 0;
            foreach (var row in state.Board)
            {
                foreach (var s in row)
                {
                    if (s == (int)Stone.Black) black++;
                    else if (s == (int)Stone.White) white++;
                }
            }

            BlackScoreText.Text = black.ToString();
            WhiteScoreText.Text = white.ToString();

            Title = TurnText.Text;
        }


        #endregion
    }
}
