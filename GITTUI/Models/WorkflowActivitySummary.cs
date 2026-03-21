namespace GITTUI.Models
{
    public class WorkflowActivitySummary
    {
        public DateTime Date { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int CancelledCount { get; set; }
        public int TotalRuns { get; set; }
    }
}