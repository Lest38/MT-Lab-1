using System;

namespace _2026_MT_Komar_A_A_Lab__.Models
{
    public class ProcessResult
    {
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Errors { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccess => ExitCode == 0;
        public bool IsTimeout { get; set; }

        public override string ToString()
        {
            return $"Command: {Command} {Arguments} | ExitCode: {ExitCode} | Duration: {DurationMs}ms | Success: {IsSuccess}";
        }
    }
}
