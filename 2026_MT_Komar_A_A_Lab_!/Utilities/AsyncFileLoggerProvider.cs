using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class AsyncFileLoggerProvider(string filePath) : ILoggerProvider, IAsyncDisposable
    {
        private readonly string _filePath = filePath;
        private AsyncFileLogger? _logger;
        private bool _disposed;

        public ILogger CreateLogger(string categoryName)
        {
            _logger = new AsyncFileLogger(_filePath);
            return _logger;
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
                _logger?.Dispose();
                _logger = null;
            }

            _disposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_logger != null)
            {
                await _logger.DisposeAsync();
                _logger = null;
            }
        }
    }
}