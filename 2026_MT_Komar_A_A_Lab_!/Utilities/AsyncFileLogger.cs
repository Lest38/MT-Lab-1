using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class AsyncFileLogger : ILogger, IAsyncDisposable, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly StreamWriter _writer;
        private bool _disposed;

        public AsyncFileLogger(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            _writer = new StreamWriter(filePath, append: true);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public async void Log<TState>(
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

            await _semaphore.WaitAsync();
            try
            {
                if (!_disposed)
                {
                    await _writer.WriteLineAsync(logEntry);
                    await _writer.FlushAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _semaphore.Wait();
                try
                {
                    _writer.Dispose();
                }
                finally
                {
                    _semaphore.Release();
                    _semaphore.Dispose();
                }
            }

            _disposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await _semaphore.WaitAsync();
            try
            {
                await _writer.DisposeAsync();
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }
}