using Microsoft.Extensions.Logging;
using System;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class FileLoggerProvider(string filePath) : ILoggerProvider
    {
        private readonly string _filePath = filePath;
        private FileLogger? _logger;
        private bool _disposed;

        public ILogger CreateLogger(string categoryName)
        {
            _logger = new FileLogger(_filePath);
            return _logger;
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
                _logger?.Dispose();
                _logger = null;
            }

            _disposed = true;
        }
    }
}