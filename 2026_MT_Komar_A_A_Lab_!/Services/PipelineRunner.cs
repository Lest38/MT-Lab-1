using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class PipelineRunner
{
    private readonly ILogger<PipelineRunner> _logger;
    private readonly ConfigurationService _configService;
    private readonly GitService _gitService;
    private readonly DotNetService _dotNetService;
    private readonly ProcessRunner _processRunner;
    private readonly Dictionary<string, DateTime> _stageTimings = new();

    public PipelineRunner(
        ILogger<PipelineRunner> logger,
        ConfigurationService configService,
        GitService gitService,
        DotNetService dotNetService,
        ProcessRunner processRunner)
    {
        _logger = logger;
        _configService = configService;
        _gitService = gitService;
        _dotNetService = dotNetService;
        _processRunner = processRunner;
    }

    public async Task<int> RunPipelineAsync(string configPath, string targetDir)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Pipeline Execution Started ===");

        var config = _configService.LoadConfiguration(configPath);
        _logger.LogInformation("Loaded pipeline with {StageCount} stages", config.Pipeline.Count);

        var stats = new PipelineStats();

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

        LogStageStart(stage, stats.StageNumber, stats.TotalStages);

        var stageStopwatch = Stopwatch.StartNew();
        var result = await ExecuteStageAsync(stage, targetDir);
        stageStopwatch.Stop();

        _stageTimings[stage.Name] = DateTime.Now;

        LogStageResult(stage, result, stageStopwatch.ElapsedMilliseconds);

        if (result.IsSuccess)
        {
            stats.SuccessfulStages++;
            await HandleSuccessfulStage(stage.Name, result.Output);
            return true;
        }

        stats.FailedStages++;
        return HandleFailedStage(stage, result);
    }

    private void LogStageStart(PipelineItem stage, int stageNumber, int totalStages)
    {
        _logger.LogInformation(
            "\n[{StageNumber}/{TotalStages}] Starting stage: {StageName}",
            stageNumber, totalStages, stage.Name);
        _logger.LogInformation("Command: {Command} {Args}", stage.Command, stage.Args);
        _logger.LogInformation("Stop on failure: {StopOnFailure}", stage.StopOnFailure);
    }

    private void LogStageResult(PipelineItem stage, ProcessResult result, long durationMs)
    {
        _logger.LogInformation(
            "Stage '{StageName}' completed in {DurationMs}ms",
            stage.Name, durationMs);

        if (!string.IsNullOrEmpty(result.Output) && result.Output.Length < 500)
        {
            _logger.LogInformation("Output: {Output}", result.Output.Trim());
        }

        if (!string.IsNullOrEmpty(result.Errors))
        {
            _logger.LogError("Errors: {Errors}", result.Errors.Trim());
        }
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
        _logger.LogError(
            "[ERROR] Stage '{StageName}' failed with ExitCode {ExitCode}",
            stage.Name, result.ExitCode);

        if (result.IsTimeout)
        {
            _logger.LogError(
                "Stage '{StageName}' timed out after {TimeoutSeconds} seconds",
                stage.Name, stage.TimeoutSeconds);
        }

        if (stage.StopOnFailure)
        {
            _logger.LogWarning(
                "Stopping pipeline due to StopOnFailure flag on stage '{StageName}'",
                stage.Name);
            return false;
        }

        _logger.LogInformation("Continuing pipeline despite failure (StopOnFailure=false)");
        return true;
    }

    private void LogPipelineSummary(TimeSpan elapsedTime, int totalStages, PipelineStats stats)
    {
        _logger.LogInformation("\n=== Pipeline Execution Summary ===");
        _logger.LogInformation(
            "Total execution time: {DurationMs}ms ({Seconds} seconds)",
            elapsedTime.TotalMilliseconds, elapsedTime.ToString(@"ss\:ff"));
        _logger.LogInformation(
            "Stages: Total={TotalStages}, Successful={SuccessfulStages}, Failed={FailedStages}",
            totalStages, stats.SuccessfulStages, stats.FailedStages);

        if (stats.FailedStages > 0)
        {
            _logger.LogWarning("Pipeline completed with {FailedStages} failed stage(s)", stats.FailedStages);
        }
        else
        {
            _logger.LogInformation("All pipeline stages completed successfully!");
        }
    }

    private async Task<ProcessResult> ExecuteStageAsync(PipelineItem stage, string targetDir)
    {
        try
        {
            string workingDir = stage.WorkingDirectory ?? targetDir;
            SetEnvironmentVariables(stage);

            return stage.Command.ToLower() switch
            {
                "git" when stage.Args.StartsWith("clone") =>
                    await _gitService.CloneAsync(workingDir, stage.Args.Replace("clone", "").Trim()),

                "git" when stage.Args.Contains("pull") =>
                    await _gitService.PullAsync(workingDir),

                "git" when stage.Args.Contains("branch") =>
                    await _gitService.GetCurrentBranchAsync(workingDir),

                "git" when stage.Args.Contains("status") =>
                    await _gitService.GetStatusAsync(workingDir),

                "dotnet" when stage.Args.Contains("clean") =>
                    await _dotNetService.CleanAsync(workingDir),

                "dotnet" when stage.Args.Contains("restore") =>
                    await _dotNetService.RestoreAsync(workingDir),

                "dotnet" when stage.Args.Contains("build") =>
                    await _dotNetService.BuildAsync(workingDir),

                "dotnet" when stage.Args.Contains("test") =>
                    await _dotNetService.TestAsync(workingDir),

                "dotnet" when stage.Args.Contains("run") =>
                    await _dotNetService.RunAsync(workingDir, waitForExit: false),

                _ => await _processRunner.RunCommandAsync(
                    stage.Command,
                    stage.Args,
                    workingDir,
                    waitForExit: true,
                    timeoutSeconds: stage.TimeoutSeconds)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing stage '{StageName}'", stage.Name);
            return new ProcessResult
            {
                Command = stage.Command,
                Arguments = stage.Args,
                ExitCode = -1,
                Errors = ex.Message,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                DurationMs = 0
            };
        }
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
            _logger.LogDebug(
                "Set environment variable: {EnvKey}={EnvValue}",
                env.Key, env.Value);
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

            _logger.LogInformation(
                "Saved artifact for stage '{StageName}' to {ArtifactFile}",
                stageName, artifactFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to save artifact for stage '{StageName}': {ErrorMessage}",
                stageName, ex.Message);
        }
    }
}