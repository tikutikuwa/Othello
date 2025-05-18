using System.Windows;

namespace Othello.Client.Wpf.Views
{
    public partial class WaitingDialog : Window
    {
        public WaitingDialog()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.Topmost = true;
            this.Activate();
        }
    }

}