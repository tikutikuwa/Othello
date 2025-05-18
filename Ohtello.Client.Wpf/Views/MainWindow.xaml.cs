using System.Text.RegularExpressions;
using System.Windows;
using Othello.Client.Wpf.Services;

namespace Othello.Client.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private bool isInitialized = false;

        public MainWindow()
        {
            isInitialized = true;
            InitializeComponent();
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

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("名前を入力してください。");
                return;
            }

            if (string.IsNullOrWhiteSpace(server))
            {
                MessageBox.Show("サーバーアドレスを入力してください。");
                return;
            }

            // APIクライアントにURLを設定
            var api = new OthelloApiClient(server);

            try
            {
                var result = await api.JoinAsync(name, matchId, isObserver, vsAI, aiLevel);
                if (result != null)
                {
                    var gameWindow = new GameWindow(result.SessionId, result.MatchId, result.AssignedColor, result.IsObserver, api);
                    gameWindow.Show();
                    Close();
                }
                else
                {
                    MessageBox.Show("マッチングに失敗しました。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("サーバーに接続できませんでした。\n" + ex.Message);
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
