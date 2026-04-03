using Microsoft.Extensions.Logging;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private FileLogger? _logger;
        private bool _disposed;

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

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