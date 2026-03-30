using Microsoft.Extensions.Logging;

namespace _2026_MT_Komar_A_A_Lab__.Utilities
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
            => new FileLogger(_filePath);

        public void Dispose() { }
    }
}