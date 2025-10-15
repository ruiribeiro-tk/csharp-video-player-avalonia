using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Specialized;

namespace VideoPlayer
{
    public partial class LoggerWindow : Window
    {
        private bool _autoScroll = true;

        public LoggerWindow()
        {
            InitializeComponent();

            // Bind the log entries to the ItemsControl
            LogItemsControl.ItemsSource = Logger.LogEntries;

            // Set default log level to Debug
            LogLevelComboBox.SelectedIndex = 0; // Debug

            // Auto-scroll when new items are added
            Logger.LogEntries.CollectionChanged += LogEntries_CollectionChanged;

            Logger.Info("Logger window opened");
        }

        private void LogEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_autoScroll && e.Action == NotifyCollectionChangedAction.Add)
            {
                // Scroll to bottom when new entries are added
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                });
            }
        }

        private void LogLevelComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LogLevelComboBox.SelectedIndex >= 0)
            {
                Logger.MinimumLevel = (LogLevel)LogLevelComboBox.SelectedIndex;
                Logger.Info($"Log level changed to: {Logger.MinimumLevel}");
            }
        }

        private void ClearLogsButton_Click(object? sender, RoutedEventArgs e)
        {
            Logger.Clear();
            Logger.Info("Logs cleared");
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            Logger.LogEntries.CollectionChanged -= LogEntries_CollectionChanged;
            Logger.Info("Logger window closed");
            base.OnClosing(e);
        }
    }
}
