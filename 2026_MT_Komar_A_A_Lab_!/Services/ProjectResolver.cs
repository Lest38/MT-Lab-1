using Microsoft.Extensions.Logging;
using System.IO;

namespace _2026_MT_Komar_A_A_Lab__.Services;

public class ProjectResolver(ILogger<ProjectResolver> logger)
{
    private readonly ILogger<ProjectResolver> _logger = logger;

    public string ResolveProjectIfNeeded(string targetDir, string args)
    {
        if (args.Contains("--project"))
        {
            return args;
        }

        var projectFiles = FindProjectFiles(targetDir);

        if (projectFiles.Length == 1)
        {
            var projectName = Path.GetFileName(projectFiles[0]);
            _logger.LogInformation("Auto-selected project: {ProjectName}", projectName);
            return $"{args} --project {projectName}";
        }

        if (projectFiles.Length > 1)
        {
            LogMultipleProjectsWarning(projectFiles);
        }

        return args;
    }

    public static string[] FindProjectFiles(string targetDir)
    {
        var projectFiles = Directory.GetFiles(targetDir, "*.csproj", SearchOption.TopDirectoryOnly);

        if (projectFiles.Length == 0)
        {
            projectFiles = Directory.GetFiles(targetDir, "*.csproj", SearchOption.AllDirectories);
        }

        return projectFiles;
    }

    private void LogMultipleProjectsWarning(string[] projectFiles)
    {
        _logger.LogWarning("Multiple projects found, please specify --project in args:");
        foreach (var proj in projectFiles)
        {
            _logger.LogWarning("  - {Project}", Path.GetFileName(proj));
        }
    }
}