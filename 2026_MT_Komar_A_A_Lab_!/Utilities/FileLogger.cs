using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _filePath;
        private static readonly object _lock = new();
        private bool _disposed;

        public FileLogger(string filePath)
        {
            _filePath = filePath;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || _disposed)
                return;

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}";

            if (exception != null)
            {
                logEntry += $"{Environment.NewLine}{exception}";
            }

            lock (_lock)
            {
                if (!_disposed)
                {
                    File.AppendAllText(_filePath, logEntry + Environment.NewLine);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Nothing to dispose, file is closed automatically
            }

            _disposed = true;
        }
    }
}