using System.Text.RegularExpressions;
using System.Windows;
using Othello.Client.Wpf.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using Othello.Core.Game;

namespace Othello.Client.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private bool isInitialized = false;
        private HubConnection? hub;
        private WaitingDialog? dialog;

        public MainWindow()
        {
            isInitialized = true;
            InitializeComponent();
            this.Closed += (s, e) => dialog?.Close();
            isInitialized = false;
        }

        private void MatchModeChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized)
            {
                MatchBox.IsEnabled = ManualMatchRadio.IsChecked == true;
            }
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string server = ServerBox.Text.Trim();
            string? matchId = ManualMatchRadio.IsChecked == true ? MatchBox.Text.Trim() : null;
            bool isObserver = ObserveRadio.IsChecked == true;
            bool vsAI = VsAIRadio.IsChecked == true;
            int aiLevel = int.TryParse(AiLevelBox.Text, out var level) ? level : 4;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(server))
            {
                MessageBox.Show("名前とサーバーアドレスを入力してください。");
                return;
            }

            var dialog = new WaitingDialog();
            dialog.Show();

            // SignalR接続
            hub = new HubConnectionBuilder()
                .WithUrl($"{server}/gamehub")
                .WithAutomaticReconnect()
                .Build();

            hub.On<JsonElement>("MatchFound", async payload =>
            {
                Console.WriteLine("MatchFound received: " + payload.ToString());

                if (!payload.TryGetProperty("matchId", out var matchIdElement) ||
                    !payload.TryGetProperty("sessionId", out var sessionIdElement) ||
                    !payload.TryGetProperty("assignedColor", out var colorElement))
                {
                    Console.WriteLine("❌ MatchFound payload に必要なキーが足りません");
                    return;
                }


                var matchId = matchIdElement.GetString()!;
                var sessionId = sessionIdElement.GetGuid();
                var assignedColor = (Stone)colorElement.GetInt32();

                await hub.InvokeAsync("JoinMatch", matchId);

                Dispatcher.Invoke(() =>
                {
                    dialog.Close();
                    var api = new OthelloApiClient(server);
                    var game = new GameWindow(sessionId, matchId, assignedColor, isObserver, api);
                    game.Show();
                    Close();
                });
            });




            try
            {
                await hub.StartAsync();

                // ConnectionId が null の場合に備えて待機（最大1秒程度）
                int retries = 0;
                while (hub.ConnectionId is null && retries++ < 10)
                {
                    await Task.Delay(100); // 100msずつ待つ
                }
                if (hub.ConnectionId is null)
                {
                    dialog.Close();
                    MessageBox.Show("接続IDの取得に失敗しました。");
                    return;
                }

                var connectionId = hub.ConnectionId!;
                var api = new OthelloApiClient(server);
                var result = await api.JoinAsync(name, matchId, isObserver, vsAI, aiLevel, connectionId);

                // サーバー側から MatchFound を待つ
                if (!result)
                {
                    dialog.Close();
                    MessageBox.Show("マッチングに失敗しました。");
                }
            }
            catch (Exception ex)
            {
                dialog.Close();
                MessageBox.Show("接続失敗: " + ex.Message);
            }
        }

        private void ObserveRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized)
            {
                if (ObserveRadio.IsChecked == true)
                {
                    ManualMatchRadio.IsChecked = true;
                    RandomMatchRadio.IsEnabled = false;
                    VsAIRadio.IsEnabled = false;
                }
                else
                {
                    RandomMatchRadio.IsEnabled = true;
                    VsAIRadio.IsEnabled = true;
                }

                MatchBox.IsEnabled = ManualMatchRadio.IsChecked == true;
            }
        }
    }
}
