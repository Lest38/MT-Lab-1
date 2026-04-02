using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new();

        public FileLogger(string filePath)
        {
            _filePath = filePath;
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
            var message = formatter(state, exception);

            lock (_lock)
            {
                File.AppendAllText(_filePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}{Environment.NewLine}");
            }
        }
    }
}