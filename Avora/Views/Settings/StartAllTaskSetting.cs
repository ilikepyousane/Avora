using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Avora.DB;
using Avora.DownloadTrack;
using Microsoft.UI.Xaml.Automation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Avora.Views.Settings
{
    public sealed class DownloadAllSetting : CheckBox
    {
        public DownloadAllSetting()
        {


            this.Content = "Запускать параллельное скачивание автоматически";

            this.Checked += StartUpSetting_Checked;
            this.Unchecked += StartUpSetting_Unchecked;
            this.Loaded += StartUpSetting_Loaded;

            // Получение стиля из ресурсов
            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;

            // Установка стиля
            this.Style = style;
            
            // Добавляем свойства доступности для экранного диктера
            AutomationProperties.SetName(this, "Запускать параллельное скачивание автоматически");
            AutomationProperties.SetHelpText(this, "Автоматически запускает параллельное скачивание всех плейлистов");
        }

        private void StartUpSetting_Loaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                var set = SettingsTable.GetSetting("downloadALL");
                this.IsChecked = set != null;
            });
        }

        private void StartUpSetting_Unchecked(object sender, RoutedEventArgs e)
        {
            SettingsTable.RemoveSetting("downloadALL");
            PlayListDownload.ResumeOnlyFirst();

        }

        private void StartUpSetting_Checked(object sender, RoutedEventArgs e)
        {
            SettingsTable.SetSetting("downloadALL", "1");
            PlayListDownload.ResumeAll();

        }
    }
}
