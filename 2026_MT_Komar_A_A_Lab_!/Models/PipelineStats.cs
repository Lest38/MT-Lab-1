namespace _2026_MT_Komar_A_A_Lab__.Models
{
    public class PipelineStats
    {
        public int StageNumber { get; set; }
        public int TotalStages { get; set; }
        public int SuccessfulStages { get; set; }
        public int FailedStages { get; set; }

        public PipelineStats()
        {
            StageNumber = 0;
            TotalStages = 0;
            SuccessfulStages = 0;
            FailedStages = 0;
        }
    }
}
