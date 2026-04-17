using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class PipelineRunner(
    ILogger<PipelineRunner> logger,
    ConfigurationService configService,
    GitService gitService,
    DotNetService dotNetService)
{
    private const string DotnetCommand = "dotnet";
    private const string GitCommand = "git";
    private const int MaxOutputLength = 500;

    private readonly ILogger<PipelineRunner> _logger = logger;
    private readonly ConfigurationService _configService = configService;
    private readonly GitService _gitService = gitService;
    private readonly DotNetService _dotNetService = dotNetService;
    private readonly Dictionary<string, DateTime> _stageTimings = [];

    public async Task<int> RunPipelineAsync(string configPath, string targetDir)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Pipeline Execution Started ===");

        var config = _configService.LoadConfiguration(configPath);
        _logger.LogInformation("Loaded pipeline with {StageCount} stages", config.Pipeline.Count);

        var stats = new PipelineStats
        {
            TotalStages = config.Pipeline.Count
        };

        foreach (var stage in config.Pipeline)
        {
            var shouldContinue = await ExecuteStageWithStats(stage, targetDir, stats);
            if (!shouldContinue)
            {
                break;
            }
        }

        LogPipelineSummary(stopwatch.Elapsed, config.Pipeline.Count, stats);
        return stats.FailedStages > 0 ? -1 : 0;
    }

    private async Task<bool> ExecuteStageWithStats(PipelineItem stage, string targetDir, PipelineStats stats)
    {
        stats.StageNumber++;

        var stageStopwatch = Stopwatch.StartNew();
        var result = await ExecuteStageAsync(stage, targetDir);
        stageStopwatch.Stop();

        _stageTimings[stage.Name] = DateTime.Now;

        LogStageExecution(stage, stats.StageNumber, stats.TotalStages, result, stageStopwatch.ElapsedMilliseconds);

        if (result.IsSuccess)
        {
            stats.SuccessfulStages++;
            await HandleSuccessfulStage(stage.Name, result.Output);
            return true;
        }

        stats.FailedStages++;
        return HandleFailedStage(stage, result);
    }

    private void LogStageExecution(PipelineItem stage, int stageNumber, int totalStages, ProcessResult result, long durationMs)
    {
        string status = result.IsSuccess ? "SUCCESS" : "FAILED";
        string outputInfo = !string.IsNullOrEmpty(result.Output) && result.Output.Length < MaxOutputLength
            ? $"\nOutput: {result.Output.Trim()}" : string.Empty;
        string errorsInfo = !string.IsNullOrEmpty(result.Errors)
            ? $"\nErrors: {result.Errors.Trim()}" : string.Empty;

        _logger.LogInformation(
            "\n[{StageNumber}/{TotalStages}] Stage: {StageName}\n" +
            "Command: {Command} {Args}\n" +
            "Stop on failure: {StopOnFailure}\n" +
            "Status: {Status} (ExitCode: {ExitCode}, Duration: {DurationMs}ms){Output}{Errors}",
            stageNumber, totalStages, stage.Name,
            stage.Command, stage.Args,
            stage.StopOnFailure,
            status, result.ExitCode, durationMs,
            outputInfo, errorsInfo);
    }

    private async Task HandleSuccessfulStage(string stageName, string output)
    {
        _logger.LogInformation("[SUCCESS] Stage '{StageName}' finished successfully", stageName);

        if (!string.IsNullOrEmpty(output))
        {
            await SaveArtifact(stageName, output);
        }
    }

    private bool HandleFailedStage(PipelineItem stage, ProcessResult result)
    {
        _logger.LogError("[ERROR] Stage '{StageName}' failed with ExitCode {ExitCode}", stage.Name, result.ExitCode);

        if (result.IsTimeout)
        {
            _logger.LogError("Stage '{StageName}' timed out after {TimeoutSeconds} seconds", stage.Name, stage.TimeoutSeconds);
        }

        if (stage.StopOnFailure)
        {
            _logger.LogWarning("Stopping pipeline due to StopOnFailure flag on stage '{StageName}'", stage.Name);
            return false;
        }

        _logger.LogInformation("Continuing pipeline despite failure (StopOnFailure=false)");
        return true;
    }

    private void LogPipelineSummary(TimeSpan elapsedTime, int totalStages, PipelineStats stats)
    {
        string summary = $"Total execution time: {elapsedTime.TotalMilliseconds}ms ({elapsedTime:ss\\:ff} seconds)\n" +
                         $"Stages: Total={totalStages}, Successful={stats.SuccessfulStages}, Failed={stats.FailedStages}";

        if (stats.FailedStages > 0)
        {
            _logger.LogWarning("=== Pipeline Execution Summary ===\n{Summary}", summary);
        }
        else
        {
            _logger.LogInformation("=== Pipeline Execution Summary ===\n{Summary}", summary);
        }
    }

    private async Task<ProcessResult> ExecuteStageAsync(PipelineItem stage, string targetDir)
    {
        try
        {
            string workingDir = stage.WorkingDirectory ?? targetDir;
            SetEnvironmentVariables(stage);

            string command = stage.Command.ToLower();
            string args = stage.Args;

            if (command == GitCommand)
            {
                return await ExecuteGitCommandAsync(stage, workingDir, args);
            }

            if (command == DotnetCommand)
            {
                return await ExecuteDotNetCommandAsync(stage, workingDir, args);
            }

            return await ProcessRunner.RunCommandAsync(
                stage.Command,
                stage.Args,
                workingDir,
                waitForExit: true,
                timeoutSeconds: stage.TimeoutSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing stage '{StageName}'", stage.Name);
            return CreateErrorResult(stage, ex.Message);
        }
    }

    private async Task<ProcessResult> ExecuteGitCommandAsync(PipelineItem stage, string workingDir, string args)
    {
        if (args.StartsWith("clone"))
        {
            return await _gitService.CloneAsync(workingDir, args.Replace("clone", "").Trim());
        }
        if (args.Contains("pull"))
        {
            return await _gitService.PullAsync(workingDir);
        }
        if (args.Contains("branch"))
        {
            return await GitService.GetCurrentBranchAsync(workingDir);
        }
        if (args.Contains("status"))
        {
            return await GitService.GetStatusAsync(workingDir);
        }

        return await ProcessRunner.RunCommandAsync(
            stage.Command,
            stage.Args,
            workingDir,
            waitForExit: true,
            timeoutSeconds: stage.TimeoutSeconds);
    }

    private async Task<ProcessResult> ExecuteDotNetCommandAsync(PipelineItem stage, string workingDir, string args)
    {
        if (args.Contains("clean"))
        {
            return await _dotNetService.CleanAsync(workingDir);
        }
        if (args.Contains("restore"))
        {
            return await _dotNetService.RestoreAsync(workingDir);
        }
        if (args.Contains("build"))
        {
            return await _dotNetService.BuildAsync(workingDir);
        }
        if (args.Contains("test"))
        {
            return await _dotNetService.TestAsync(workingDir);
        }
        if (args.Contains("run"))
        {
            return await _dotNetService.RunAsync(workingDir, waitForExit: true);
        }

        return await ProcessRunner.RunCommandAsync(
            stage.Command,
            stage.Args,
            workingDir,
            waitForExit: true,
            timeoutSeconds: stage.TimeoutSeconds);
    }

    private static ProcessResult CreateErrorResult(PipelineItem stage, string errorMessage)
    {
        return new ProcessResult
        {
            Command = stage.Command,
            Arguments = stage.Args,
            ExitCode = -1,
            Errors = errorMessage,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now,
            DurationMs = 0
        };
    }

    private void SetEnvironmentVariables(PipelineItem stage)
    {
        if (stage.Environment == null || stage.Environment.Count == 0)
        {
            return;
        }

        foreach (var env in stage.Environment)
        {
            Environment.SetEnvironmentVariable(env.Key, env.Value);
            _logger.LogDebug("Set environment variable: {EnvKey}={EnvValue}", env.Key, env.Value);
        }
    }

    private async Task SaveArtifact(string stageName, string output)
    {
        try
        {
            var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
            Directory.CreateDirectory(artifactsDir);

            var artifactFile = Path.Combine(artifactsDir, $"{stageName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            await File.WriteAllTextAsync(artifactFile, output);

            _logger.LogInformation("Saved artifact for stage '{StageName}' to {ArtifactFile}", stageName, artifactFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save artifact for stage '{StageName}'", stageName);
        }
    }
}