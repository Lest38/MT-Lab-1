using System.Text.Json.Serialization;

namespace _2026_MT_Komar_A_A_Lab__.Models
{
    public class PipelineSettings
    {
        [JsonPropertyName("continueOnError")]
        public bool ContinueOnError { get; set; } = false;

        [JsonPropertyName("maxParallelStages")]
        public int MaxParallelStages { get; set; } = 1;

        [JsonPropertyName("artifactsPath")]
        public string? ArtifactsPath { get; set; }

    }
}
