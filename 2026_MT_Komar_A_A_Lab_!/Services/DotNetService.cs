using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class DotNetService(
    ProjectResolver projectResolver,
    ILogger<DotNetService> logger)
{
    private readonly ProjectResolver _projectResolver = projectResolver;
    private readonly ILogger<DotNetService> _logger = logger;

    public async Task<ProcessResult> RunCommandAsync(string targetDir, string args, bool waitForExit = true)
    {
        args = _projectResolver.ResolveProjectIfNeeded(targetDir, args);

        _logger.LogInformation("Running: dotnet {Args} in {TargetDir}", args, targetDir);
        return await ProcessRunner.RunCommandAsync(
            "dotnet",
            args,
            targetDir,
            waitForExit,
            timeoutSeconds: TimeoutDefaults.Default
        );
    }

    public async Task<ProcessResult> CleanAsync(string targetDir)
    {
        _logger.LogInformation("Cleaning solution in {TargetDir}", targetDir);
        return await RunCommandAsync(targetDir, "clean");
    }

    public async Task<ProcessResult> RestoreAsync(string targetDir)
    {
        _logger.LogInformation("Restoring packages in {TargetDir}", targetDir);
        return await RunCommandAsync(targetDir, "restore");
    }

    public async Task<ProcessResult> BuildAsync(string targetDir)
    {
        _logger.LogInformation("Building solution in {TargetDir}", targetDir);
        return await RunCommandAsync(targetDir, "build --configuration Release");
    }

    public async Task<ProcessResult> TestAsync(string targetDir)
    {
        _logger.LogInformation("Running tests in {TargetDir}", targetDir);
        return await RunCommandAsync(targetDir, "test --configuration Release --no-build");
    }

    public async Task<ProcessResult> RunAsync(string targetDir, bool waitForExit = true)
    {
        _logger.LogInformation("Starting application in {TargetDir}", targetDir);
        return await RunCommandAsync(targetDir, "run --configuration Release", waitForExit);
    }

    public async Task<ProcessResult> RunProjectAsync(string targetDir, string projectName, bool waitForExit = true)
    {
        _logger.LogInformation("Starting project {ProjectName} in {TargetDir}", projectName, targetDir);
        return await RunCommandAsync(targetDir, $"run --project {projectName} --configuration Release", waitForExit);
    }
}