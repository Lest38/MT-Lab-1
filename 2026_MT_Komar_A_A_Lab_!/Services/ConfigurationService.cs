using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class ConfigurationService(ILogger<ConfigurationService> logger, ConfigReader configReader)
{
    private readonly ILogger<ConfigurationService> _logger = logger;
    private readonly ConfigReader _configReader = configReader;

    public PipelineConfig LoadConfiguration(string configPath)
    {
        var config = _configReader.ReadConfig(configPath);

        if (config == null)
        {
            _logger.LogError("Failed to load configuration from {ConfigPath}", configPath);
            throw new InvalidOperationException($"Failed to load configuration from {configPath}");
        }

        _logger.LogInformation("Loaded {StageCount} stages from configuration", config.Pipeline.Count);

        foreach (var stage in config.Pipeline)
        {
            _logger.LogDebug(
                "Stage: {StageName} | Command: {Command} {Args} | StopOnFailure: {StopOnFailure}",
                stage.Name, stage.Command, stage.Args, stage.StopOnFailure);
        }

        return config;
    }
}