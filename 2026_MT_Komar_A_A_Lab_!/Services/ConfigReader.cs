using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class ConfigReader
{
    private readonly ILogger<ConfigReader> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigReader(ILogger<ConfigReader> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public PipelineConfig? ReadConfig(string filePath)
    {
        try
        {
            _logger.LogInformation("Reading configuration from: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Configuration file not found: {FilePath}", filePath);
                return null;
            }

            string jsonString = File.ReadAllText(filePath);
            _logger.LogDebug("Loaded JSON: {JsonString}", jsonString);

            var config = JsonSerializer.Deserialize<PipelineConfig>(jsonString, _jsonOptions);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize configuration");
                return null;
            }

            ValidateConfig(config);

            _logger.LogInformation("Successfully loaded pipeline with {Count} stages", config.Pipeline.Count);
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in configuration file");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration file");
            return null;
        }
    }

    private static void ValidateConfig(PipelineConfig config)
    {
        if (config.Pipeline == null || config.Pipeline.Count == 0)
        {
            throw new InvalidDataException("Pipeline must contain at least one stage");
        }

        foreach (var stage in config.Pipeline)
        {
            if (string.IsNullOrEmpty(stage.Name))
            {
                throw new InvalidDataException("Each pipeline stage must have a name");
            }

            if (string.IsNullOrEmpty(stage.Command))
            {
                throw new InvalidDataException($"Stage '{stage.Name}' must have a command");
            }
        }
    }
}