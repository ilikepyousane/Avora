using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Avora.Views.Settings
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void DebugConsole_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.contentFrame.Navigate(typeof(DebugConsole));
        }
    }
}
