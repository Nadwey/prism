using System.Windows;
using System.Windows.Input;

namespace prism.Windows
{
    public partial class MessageBox : Window
    {
        public MessageBox() { InitializeComponent(); }
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        public void ShowMessage(string Message)
        {
            messagelabel.Text = Message;
            this.ShowDialog(); // instead of this.Show() : Waits until this window is closed.
        }

        private void okbutton_click(object sender, RoutedEventArgs e) => this.Hide();
    }
}
