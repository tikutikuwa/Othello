using System.Text.RegularExpressions;
using System.Windows;
using Othello.Client.Wpf.Services;

namespace Othello.Client.Wpf.Views
{
    public partial class MainWindow : Window
    {
        private readonly OthelloApiClient api = new();
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
                // チェックボックスに応じて入力欄の有効化切替
                MatchBox.IsEnabled = ManualMatchRadio.IsChecked == true;
            }
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string? matchId = ManualMatchRadio.IsChecked == true ? MatchBox.Text.Trim() : null;
            bool isObserver = ObserveRadio.IsChecked == true;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("名前を入力してください。");
                return;
            }

            if (ManualMatchRadio.IsChecked == true && !Regex.IsMatch(matchId ?? "", @"^\d{4}$"))
            {
                MessageBox.Show("マッチIDは4桁の数字で入力してください。");
                return;
            }

            if (isObserver && matchId == null)
            {
                MessageBox.Show("観戦の場合はマッチIDを指定してください。");
                return;
            }

            try
            {
                var result = await api.JoinAsync(name, matchId, isObserver);
                if (result != null)
                {
                    var gameWindow = new GameWindow(result.SessionId, result.MatchId);
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

    }
}
