using _2026_MT_Komar_A_A_Lab__.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace _2026_MT_Komar_A_A_Lab__.Services
{
    public class DotNetService
    {
        private readonly ProcessRunner _processRunner;
        private readonly ILogger<DotNetService> _logger;

        public DotNetService(ProcessRunner processRunner, ILogger<DotNetService> logger)
        {
            _processRunner = processRunner;
            _logger = logger;
        }

        public async Task<ProcessResult> RunCommandAsync(string targetDir, string args, bool waitForExit = true)
        {
            return await _processRunner.RunCommandAsync(
                "dotnet",
                args,
                targetDir,
                waitForExit,
                timeoutSeconds: 300
            );
        }

        public async Task<ProcessResult> CleanAsync(string targetDir)
        {
            _logger.LogInformation("Cleaning solution in {TargetDir}", targetDir);
            return await RunCommandAsync(targetDir, "clean");
        }

        public async Task<ProcessResult> BuildAsync(string targetDir)
        {
            _logger.LogInformation("Building solution in {TargetDir}", targetDir);
            return await RunCommandAsync(targetDir, "build");
        }

        public async Task<ProcessResult> TestAsync(string targetDir)
        {
            _logger.LogInformation("Running tests in {TargetDir}", targetDir);
            return await RunCommandAsync(targetDir, "test");
        }

        public async Task<ProcessResult> RunAsync(string targetDir, bool waitForExit = false)
        {
            _logger.LogInformation("Starting application in {TargetDir}", targetDir);
            return await RunCommandAsync(targetDir, "run", waitForExit);
        }

        public async Task<ProcessResult> RestoreAsync(string targetDir)
        {
            _logger.LogInformation("Restoring packages in {TargetDir}", targetDir);
            return await RunCommandAsync(targetDir, "restore");
        }
    }
}
