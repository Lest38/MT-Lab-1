using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class GitService
{
    private readonly ProcessRunner _processRunner;
    private readonly ILogger<GitService> _logger;

    public GitService(ProcessRunner processRunner, ILogger<GitService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<ProcessResult> CloneAsync(string targetDir, string repoUrl)
    {
        string cloneTargetDir = targetDir;
        string? repoSubDir = null;

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
                _logger.LogWarning("Target directory {TargetDir} is not empty. Found {FileCount} non-log files.",
                    cloneTargetDir, files.Length);

                return new ProcessResult
                {
                    Command = "git",
                    Arguments = $"clone {repoUrl}",
                    ExitCode = 0,
                    Output = $"Directory {cloneTargetDir} already exists and contains {files.Length} files. Skipping clone.",
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                };
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
        return await _processRunner.RunCommandAsync(
            "git",
            $"clone {repoUrl} {cloneTargetDir}",
            ".",
            timeoutSeconds: 300
        );
    }

    public async Task<ProcessResult> PullAsync(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            _logger.LogError("Directory does not exist: {TargetDir}", targetDir);
            return new ProcessResult
            {
                Command = "git",
                Arguments = "pull",
                ExitCode = -1,
                Errors = $"Directory {targetDir} does not exist",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }

        _logger.LogInformation("Pulling latest changes in {TargetDir}", targetDir);
        return await _processRunner.RunCommandAsync(
            "git",
            "pull",
            targetDir,
            timeoutSeconds: 60
        );
    }

    public async Task<ProcessResult> GetCurrentBranchAsync(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            return new ProcessResult
            {
                Command = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                ExitCode = -1,
                Errors = $"Directory {targetDir} does not exist",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }

        return await _processRunner.RunCommandAsync(
            "git",
            "rev-parse --abbrev-ref HEAD",
            targetDir
        );
    }

    public async Task<ProcessResult> GetStatusAsync(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            return new ProcessResult
            {
                Command = "git",
                Arguments = "status --short",
                ExitCode = -1,
                Errors = $"Directory {targetDir} does not exist",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }

        return await _processRunner.RunCommandAsync(
            "git",
            "status --short",
            targetDir
        );
    }
}