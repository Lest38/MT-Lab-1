using System.Text.Json.Serialization;

namespace _2026_MT_Komar_A_A_Lab__.Models
{
    public class PipelineConfig
    {
        [JsonPropertyName("pipeline")]
        public List<PipelineItem> Pipeline { get; set; } = new();

        [JsonPropertyName("settings")]
        public PipelineSettings? Settings { get; set; }
    }
}
