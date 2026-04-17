using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class GitService(ILogger<GitService> logger)
{
    private readonly ILogger<GitService> _logger = logger;

    public async Task<ProcessResult> CloneAsync(string targetDir, string repoUrl)
    {
        string cloneTargetDir = targetDir;
        string? repoSubDir;

        var parts = repoUrl.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            repoUrl = parts[0];
            repoSubDir = parts[1];
            cloneTargetDir = Path.Combine(targetDir, repoSubDir);
        }

        if (Directory.Exists(cloneTargetDir))
        {
            var files = Directory.GetFileSystemEntries(cloneTargetDir).ToArray();

            if (files.Length > 0)
            {
                _logger.LogWarning("Target directory {TargetDir} is not empty. Found {FileCount} files.",
                    cloneTargetDir, files.Length);

                return CreateSuccessResult("git", $"clone {repoUrl}",
                    $"Directory {cloneTargetDir} already exists and contains {files.Length} files. Skipping clone.");
            }
        }
        else
        {
            var parentDir = Path.GetDirectoryName(cloneTargetDir);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
                _logger.LogInformation("Created parent directory: {ParentDir}", parentDir);
            }
        }

        _logger.LogInformation("Cloning repository {RepoUrl} to {TargetDir}", repoUrl, cloneTargetDir);
        return await ProcessRunner.RunCommandAsync(
            "git",
            $"clone {repoUrl} {cloneTargetDir}",
            ".",
            timeoutSeconds: TimeoutDefaults.Clone
        );
    }

    public async Task<ProcessResult> PullAsync(string targetDir)
    {
        if (!TryEnsureDirectoryExists(targetDir, out var errorResult))
            return errorResult!;

        _logger.LogInformation("Pulling latest changes in {TargetDir}", targetDir);
        return await ProcessRunner.RunCommandAsync(
            "git",
            "pull",
            targetDir,
            timeoutSeconds: TimeoutDefaults.Pull
        );
    }

    public static async Task<ProcessResult> GetCurrentBranchAsync(string targetDir)
    {
        if (!TryEnsureDirectoryExists(targetDir, out var errorResult))
            return errorResult!;

        return await ProcessRunner.RunCommandAsync(
            "git",
            "rev-parse --abbrev-ref HEAD",
            targetDir
        );
    }

    public static async Task<ProcessResult> GetStatusAsync(string targetDir)
    {
        if (!TryEnsureDirectoryExists(targetDir, out var errorResult))
            return errorResult!;

        return await ProcessRunner.RunCommandAsync(
            "git",
            "status --short",
            targetDir
        );
    }

    private static bool TryEnsureDirectoryExists(string targetDir, out ProcessResult? errorResult)
    {
        if (!Directory.Exists(targetDir))
        {
            errorResult = CreateErrorResult("command", targetDir, "Directory does not exist");
            return false;
        }
        errorResult = null;
        return true;
    }

    private static ProcessResult CreateErrorResult(string command, string targetDir, string errorMessage)
    {
        return new ProcessResult
        {
            Command = "git",
            Arguments = command,
            ExitCode = -1,
            Errors = $"{errorMessage}: {targetDir}",
            StartTime = DateTime.Now,
            EndTime = DateTime.Now
        };
    }

    private static ProcessResult CreateSuccessResult(string command, string arguments, string output)
    {
        return new ProcessResult
        {
            Command = command,
            Arguments = arguments,
            ExitCode = 0,
            Output = output,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now
        };
    }
}