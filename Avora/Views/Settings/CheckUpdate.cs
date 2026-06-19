using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation;

namespace Avora.Views.Settings
{
    class CheckUpdate : Button
    {
        public CheckUpdate()
        {
            this.CornerRadius = new CornerRadius(8);
            Click += CheckUpdate_Click;
            Style style = Application.Current.Resources["DefaultButtonStyle"] as Style;
            this.Content = "Проверить обновления";
            
            // Добавляем свойства доступности для экранного диктера
            AutomationProperties.SetName(this, "Проверить обновления");
            AutomationProperties.SetHelpText(this, "Проверяет наличие новых версий приложения");
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                this.Content = "Проверка...";

                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var currentVersion = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}";
                var appUpdater = new SetupLib.AppUpdater(currentVersion);
                appUpdater.SelectedPackageType = SetupLib.PackageType.ZIP;
                var updateAvailable = await appUpdater.CheckForUpdates();

                if (updateAvailable && appUpdater._currentReleaseInfo.Assets.ContainsKey(SetupLib.PackageType.ZIP))
                {
                    MainWindow.PendingAppUpdater = appUpdater;

                    var settingsPage = FindParent<SettingsPage>();
                    if (settingsPage != null)
                    {
                        settingsPage.ShowUpdatePanel(appUpdater);
                    }
                }
                else
                {
                    Flyout myFlyout = new Flyout();
                    TextBlock firstItem = new TextBlock { Text = "Обновлений не найдено" };
                    AutomationProperties.SetName(firstItem, "Результат проверки обновлений");
                    AutomationProperties.SetHelpText(firstItem, "Информирует о том, что обновления не найдены");
                    myFlyout.Content = firstItem;
                    myFlyout.ShowAt(this);

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(10);
                    timer.Tick += (s, ev) =>
                    {
                        myFlyout.Hide();
                        timer.Stop();
                        this.IsEnabled = true;
                        this.Content = "Проверить обновления";
                    };
                    timer.Start();
                }

                if (updateAvailable)
                {
                    this.Content = "Проверить обновления";
                    this.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MenuFlyout myFlyout = new MenuFlyout();
                MenuFlyoutItem firstItem = new MenuFlyoutItem { Text = $"Произошла ошибка. Проверьте настройки сети.\n{ex.Message}" };
                AutomationProperties.SetName(firstItem, "Ошибка проверки обновлений");
                AutomationProperties.SetHelpText(firstItem, $"Произошла ошибка при проверке обновлений: {ex.Message}");
                myFlyout.Items.Add(firstItem);
                myFlyout.ShowAt(this);

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(10);
                timer.Tick += (s, ev) =>
                {
                    myFlyout.Hide();
                    timer.Stop();
                    this.IsEnabled = true;
                    this.Content = "Проверить обновления";
                };
                timer.Start();
            }
        }

        private T FindParent<T>() where T : class
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                if (parent is T typed)
                    return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
