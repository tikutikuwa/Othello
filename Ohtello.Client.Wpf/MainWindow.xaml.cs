using System.Windows;
using Othello.Client.Wpf.Services;

namespace Othello.Client.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly OthelloApiClient api = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string matchId = MatchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("名前を入力してください。");
                return;
            }

            try
            {
                var result = await api.JoinAsync(name, matchId);
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
