using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Avora.DB;
using Microsoft.UI.Xaml.Automation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Avora.Views.Settings
{
    public sealed class BackDropSetting : CheckBox
    {
        public BackDropSetting()
        {


            this.Content = "Выключить прозрачность";

            this.Checked += StartUpSetting_Checked;
            this.Unchecked += StartUpSetting_Unchecked;
            this.Loaded += StartUpSetting_Loaded;

            // Получение стиля из ресурсов
            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;

            // Установка стиля
            this.Style = style;
            
            // Добавляем свойства доступности для экранного диктера
            AutomationProperties.SetName(this, "Выключить прозрачность");
            AutomationProperties.SetHelpText(this, "Включает или выключает эффект прозрачности в приложении");
        }

        private void StartUpSetting_Loaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                var set = SettingsTable.GetSetting("backDrop");
                this.IsChecked = set != null;
            });
        }

        private void StartUpSetting_Unchecked(object sender, RoutedEventArgs e)
        {

            MainWindow.dispatcherQueue.TryEnqueue(() =>
            {
                SettingsTable.RemoveSetting("backDrop");

            });
        }

        private void StartUpSetting_Checked(object sender, RoutedEventArgs e)
        {

            MainWindow.dispatcherQueue.TryEnqueue(() =>
            {
                SettingsTable.SetSetting("backDrop", "1");
            });
        }
    }
}
