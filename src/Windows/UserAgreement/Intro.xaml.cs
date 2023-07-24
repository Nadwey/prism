using System.Windows;
using System.Windows.Controls;

namespace prism.Windows.UserAgreement
{
    public partial class Intro : Page
    {
        public Intro()
        {
            InitializeComponent();
        }

        private void Page_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e) => FixPositioning();
        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e) => FixPositioning();

        void FixPositioning()
        {
            double TopMargin = ActualHeight / 8;
            headerhandler.Margin = new Thickness(0,TopMargin,0,0);
        }
    }
}
