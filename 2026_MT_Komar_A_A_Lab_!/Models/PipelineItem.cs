using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace _2026_MT_Komar_A_A_Lab__.Models
{
    public class PipelineItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public string Args { get; set; } = string.Empty;

        [JsonPropertyName("stopOnFailure")]
        public bool StopOnFailure { get; set; } = true;

        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 300;

        [JsonPropertyName("workingDirectory")]
        public string? WorkingDirectory { get; set; }

        [JsonPropertyName("environment")]
        public Dictionary<string, string>? Environment { get; set; }
    }
}