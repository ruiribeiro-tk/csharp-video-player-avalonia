using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VideoPlayer
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogEntry : INotifyPropertyChanged
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");

        public string LevelString => Level.ToString().ToUpper();

        public string LevelColor => Level switch
        {
            LogLevel.Debug => "#A0A0A0",
            LogLevel.Info => "#FFFFFF",
            LogLevel.Warning => "#FFA500",
            LogLevel.Error => "#FF4444",
            _ => "#FFFFFF"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public LogEntry(DateTime timestamp, LogLevel level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
        }
    }

    public static class Logger
    {
        private static ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        private static LogLevel _minimumLevel = LogLevel.Debug;

        public static ObservableCollection<LogEntry> LogEntries => _logEntries;

        public static LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        private static void Log(LogLevel level, string message)
        {
            if (level < _minimumLevel)
                return;

            var entry = new LogEntry(DateTime.Now, level, message);

            // Also write to console for debugging
            Console.WriteLine($"[{entry.LevelString}] {entry.FormattedTimestamp} - {message}");

            // Add to observable collection on UI thread
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _logEntries.Add(entry);

                // Keep only last 1000 entries to prevent memory issues
                if (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveAt(0);
                }
            });
        }

        public static void Clear()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _logEntries.Clear();
            });
        }
    }
}
