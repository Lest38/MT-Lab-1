using _2026_MT_Komar_A_A_Lab__.Services;
using _2026_MT_Komar_A_A_Lab__.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(new string('═', 60));

        if (args.Length < 2)
        {
            Console.WriteLine("\nError: Missing required arguments");
            Console.WriteLine("\nUsage: CICD.SyncVersion <config.json> <target_dir>");
            Console.WriteLine("\nExample:");
            Console.WriteLine("  dotnet run -- config.json test_target");
            return;
        }

        var configPath = args[0];
        var targetDir = args[1];

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"\nConfig file not found: {configPath}");
            return;
        }

        if (!Directory.Exists(targetDir))
        {
            Console.WriteLine($"\nCreating target directory: {targetDir}");
            Directory.CreateDirectory(targetDir);
        }

        var logFilePath = LogFileGenerator.GenerateLogFilePath(targetDir);
        Console.WriteLine($"\nLog file: {logFilePath}");

        var services = new ServiceCollection();
        ConfigureServices(services, logFilePath);

        var serviceProvider = services.BuildServiceProvider();
        var pipelineRunner = serviceProvider.GetRequiredService<PipelineRunner>();
        var logger = serviceProvider.GetRequiredService<ILogger<PipelineRunner>>();

        try
        {
            int exitCode = await pipelineRunner.RunPipelineAsync(configPath, targetDir);

            Console.WriteLine(new string('═', 60));
            if (exitCode == 0)
            {
                Console.WriteLine("Pipeline completed successfully!");
            }
            else
            {
                Console.WriteLine($"Pipeline completed with errors. Exit code: {exitCode}");
            }

            logger.LogInformation("Pipeline completed with exit code: {ExitCode}", exitCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline execution failed");
            Console.WriteLine($"\nFatal error: {ex.Message}");
            Console.WriteLine($"\nStack trace: {ex.StackTrace}");
        }

        Console.WriteLine($"\nFinished at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void ConfigureServices(IServiceCollection services, string logFilePath)
    {
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddProvider(new FileLoggerProvider(logFilePath));
            configure.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSingleton<ConfigReader>();
        services.AddSingleton<ProjectResolver>();
        services.AddTransient<ProcessRunner>();
        services.AddTransient<PipelineRunner>();
        services.AddTransient<GitService>();
        services.AddTransient<DotNetService>();
        services.AddTransient<ConfigurationService>();
    }
}