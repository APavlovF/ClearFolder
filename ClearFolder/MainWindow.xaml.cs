using System.Windows;

namespace ClearFolder
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void ButtonIncrease_OnClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).IsAddDays = true;

        }

        void ButtonDecrease_OnClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel) DataContext).IsAddDays = false;
        }

        void IsSelectedClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).SelectedChanged();
        }
    }
}
