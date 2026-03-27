using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;

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
        _logger.LogInformation("Cloning repository {RepoUrl} to {TargetDir}", repoUrl, targetDir);
        return await _processRunner.RunCommandAsync(
            "git",
            $"clone {repoUrl} {targetDir}",
            ".",
            timeoutSeconds: 300
        );
    }

    public async Task<ProcessResult> PullAsync(string targetDir)
    {
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
        return await _processRunner.RunCommandAsync(
            "git",
            "rev-parse --abbrev-ref HEAD",
            targetDir
        );
    }

    public async Task<ProcessResult> GetCommitHashAsync(string targetDir)
    {
        return await _processRunner.RunCommandAsync(
            "git",
            "rev-parse HEAD",
            targetDir
        );
    }

    public async Task<ProcessResult> GetStatusAsync(string targetDir)
    {
        return await _processRunner.RunCommandAsync(
            "git",
            "status --short",
            targetDir
        );
    }
}