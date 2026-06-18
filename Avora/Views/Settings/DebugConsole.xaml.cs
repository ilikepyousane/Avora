using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Avora.Views.Settings
{
    public sealed partial class DebugConsole : Page
    {
        private static readonly ObservableCollection<string> _logBuffer = new();
        private const int MaxLines = 500;

        public DebugConsole()
        {
            this.InitializeComponent();

            foreach (var line in _logBuffer)
            {
                LogTextBlock.Text += line + Environment.NewLine;
            }
            UpdateStatus();
        }

        internal void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[{timestamp}] {message}";

            _logBuffer.Add(line);
            if (_logBuffer.Count > MaxLines)
            {
                _logBuffer.RemoveAt(0);
            }

            this.DispatcherQueue.TryEnqueue(() =>
            {
                LogTextBlock.Text += line + Environment.NewLine;
                UpdateStatus();

                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollViewer.ChangeView(0, double.MaxValue, 0);
                }
            });
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Строк: {_logBuffer.Count}";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.contentFrame.CanGoBack)
                MainWindow.contentFrame.GoBack();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _logBuffer.Clear();
            LogTextBlock.Text = "";
            UpdateStatus();
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(LogTextBlock.Text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            StatusText.Text = "Скопировано!";
            await System.Threading.Tasks.Task.Delay(2000);
            UpdateStatus();
        }
    }
}
