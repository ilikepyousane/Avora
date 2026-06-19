using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SetupLib;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Avora.Views.Settings
{
    public sealed partial class SettingsPage : Page
    {
        private AppUpdater _appUpdater;

        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPendingUpdate();
        }

        private void LoadPendingUpdate()
        {
            _appUpdater = MainWindow.PendingAppUpdater;
            if (_appUpdater == null) return;
            ShowUpdatePanel(_appUpdater);
        }

        public void ShowUpdatePanel(AppUpdater appUpdater)
        {
            _appUpdater = appUpdater;
            UpdateVersionText.Text = appUpdater.version;
            UpdateSizeText.Text = $"{Math.Round(appUpdater.sizeFile / 1024.0 / 1024, 2)} МБ";
            UpdateDescriptionText.Text = appUpdater.Tit;
            UpdatePanel.Visibility = Visibility.Visible;
        }

        private async void UpdateInstallButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButton.IsEnabled = false;
            UpdateInstallButton.Content = "Загрузка...";
            UpdateProgressBar.Visibility = Visibility.Visible;
            UpdateProgressText.Visibility = Visibility.Visible;

            _appUpdater.DownloadProgressChanged += AppUpdater_DownloadProgressChanged;
            await _appUpdater.DownloadAndOpenFile(true, false, PathInstallZIP: AppContext.BaseDirectory);
        }

        private void AppUpdater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateProgressBar.Value = e.Percentage;
                UpdateProgressText.Text = $"{Math.Round(e.BytesDownloaded / 1024.0 / 1024, 2)} МБ";
            });
        }
    }
}
