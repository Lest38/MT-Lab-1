using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services
{
    public class ProcessRunner
    {
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResult> RunCommandAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            bool waitForExit = true,
            int timeoutSeconds = 0)
        {
            var result = InitializeResult(fileName, arguments, workingDirectory);

            var process = StartProcess(fileName, arguments, workingDirectory, result);
            if (process == null)
            {
                return result;
            }

            if (!waitForExit)
            {
                HandleBackgroundProcess(result);
                return result;
            }

            await HandleWaitingProcess(process, result, timeoutSeconds);
            return result;
        }

        public async Task<ProcessResult> RunCommandWithOutputAsync(
            string fileName,
            string arguments,
            string workingDirectory)
        {
            return await RunCommandAsync(fileName, arguments, workingDirectory, waitForExit: true);
        }

        private ProcessResult InitializeResult(string fileName, string arguments, string workingDirectory)
        {
            return new ProcessResult
            {
                Command = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                StartTime = DateTime.Now
            };
        }

        private Process? StartProcess(string fileName, string arguments, string workingDirectory, ProcessResult result)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processInfo);
            if (process == null)
            {
                result.ExitCode = -1;
                result.Errors = "Failed to start process";
                result.EndTime = DateTime.Now;
                result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
                _logger.LogError("Failed to start process: {FileName} {Arguments}", fileName, arguments);
                return null;
            }

            result.ProcessId = process.Id;
            _logger.LogInformation(
                "Started process: {FileName} {Arguments} (PID: {ProcessId}) in {WorkingDirectory}",
                fileName, arguments, process.Id, workingDirectory);

            return process;
        }

        private void HandleBackgroundProcess(ProcessResult result)
        {
            result.ExitCode = 0;
            result.EndTime = DateTime.Now;
            result.DurationMs = 0;
            _logger.LogInformation(
                "Process started in background (PID: {ProcessId})",
                result.ProcessId);
        }

        private async Task HandleWaitingProcess(Process process, ProcessResult result, int timeoutSeconds)
        {
            bool hasTimeout = timeoutSeconds > 0;
            bool exited = true;

            if (hasTimeout)
            {
                exited = await WaitForProcessWithTimeout(process, timeoutSeconds, result);
            }
            else
            {
                await process.WaitForExitAsync();
            }

            if (hasTimeout && !exited)
            {
                return;
            }

            await CaptureProcessOutput(process, result);
            FinalizeResult(result);
            LogProcessResult(result);
        }

        private async Task<bool> WaitForProcessWithTimeout(Process process, int timeoutSeconds, ProcessResult result)
        {
            bool exited = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));

            if (!exited)
            {
                _logger.LogError(
                    "Process timed out after {TimeoutSeconds} seconds. Killing process (PID: {ProcessId})...",
                    timeoutSeconds, process.Id);

                process.Kill();
                result.IsTimeout = true;
                result.Errors = $"Process timed out after {timeoutSeconds} seconds";
                result.ExitCode = -1;
                result.EndTime = DateTime.Now;
                result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
            }

            return exited;
        }

        private static async Task CaptureProcessOutput(Process process, ProcessResult result)
        {
            result.Output = await process.StandardOutput.ReadToEndAsync();
            result.Errors = await process.StandardError.ReadToEndAsync();
            result.ExitCode = process.ExitCode;
        }

        private static void FinalizeResult(ProcessResult result)
        {
            result.EndTime = DateTime.Now;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
        }

        private void LogProcessResult(ProcessResult result)
        {
            if (!string.IsNullOrEmpty(result.Output))
            {
                _logger.LogDebug("Output: {Output}", result.Output);
            }

            if (!string.IsNullOrEmpty(result.Errors))
            {
                _logger.LogError("Errors: {Errors}", result.Errors);
            }

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Process completed successfully (PID: {ProcessId}, Duration: {DurationMs}ms)",
                    result.ProcessId, result.DurationMs);
            }
            else
            {
                _logger.LogError(
                    "Process failed with ExitCode: {ExitCode} (PID: {ProcessId}, Duration: {DurationMs}ms)",
                    result.ExitCode, result.ProcessId, result.DurationMs);
            }
        }
    }
}