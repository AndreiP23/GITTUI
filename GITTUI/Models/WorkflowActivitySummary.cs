namespace GITTUI.Models
{
    public class WorkflowActivitySummary
    {
        public required DateTime Date { get; init; }
        public required int SuccessCount { get; init; }
        public required int FailureCount { get; init; }
        public required int CancelledCount { get; init; }
        public required int TotalRuns { get; init; }
    }
}