using _2026_MT_Komar_A_A_Lab__.Models;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class ProcessRunner : IDisposable
{
    private Process? _currentProcess;
    private bool _disposed;

    public static async Task<ProcessResult> RunCommandAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        bool waitForExit = true,
        int timeoutSeconds = 0)
    {
        var result = new ProcessResult
        {
            Command = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            StartTime = DateTime.Now
        };

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            result.ProcessId = process.Id;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!waitForExit)
            {
                result.ExitCode = 0;
                result.EndTime = DateTime.Now;
                return result;
            }

            bool exited;

            if (timeoutSeconds > 0)
            {
                exited = process.WaitForExit(timeoutSeconds * 1000);
            }
            else
            {
                await process.WaitForExitAsync();
                exited = true;
            }

            if (!exited)
            {
                process.Kill();
                result.IsTimeout = true;
                result.ExitCode = -1;
                result.Errors = $"Timeout after {timeoutSeconds} seconds";
            }
            else
            {
                result.ExitCode = process.ExitCode;
            }

            result.Output = outputBuilder.ToString();
            result.Errors = errorBuilder.ToString();
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.Errors = ex.Message;
        }
        finally
        {
            result.EndTime = DateTime.Now;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
        }

        return result;
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

        if (disposing && _currentProcess != null && !_currentProcess.HasExited)
        {
            try
            {
                _currentProcess.Kill();
            }
            catch
            {
                // The process have already exited or we can't kill it, so we ignore exceptions here
            }
            _currentProcess.Dispose();
            _currentProcess = null;
        }

        _disposed = true;
    }
}