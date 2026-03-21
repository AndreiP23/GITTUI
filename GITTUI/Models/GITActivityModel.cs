namespace GITTUI.Models
{
    public enum WorkflowStatus
    {
        Queued,
        Pending,
        Waiting,
        InProgress,
        Completed,
        Unknown
    }

    public enum WorkflowConclusion
    {
        Success,
        Failure,
        StartupFailure,
        Cancelled,
        Aborted,
        Skipped,
        TimedOut,
        Unknown
    }

    public enum WorkflowEvent
    {
        Push,
        PullRequest,
        Unknown
    }

    internal class GITActivityModel
    {
        public required string WorkflowName { get; init; }
        public required WorkflowStatus Status { get; init; }
        public required WorkflowConclusion Conclusion { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required WorkflowEvent Event { get; init; }
        public long RunId { get; init; }
        public string? LogsUrl { get; init; }

        public bool HasLogs => !string.IsNullOrEmpty(LogsUrl);

        public string StatusIcon => StatusIconMapper.GetIcon(Status, Conclusion);

        // Optionally, add a static factory to parse from strings if needed
        public static GITActivityModel From(
            string workflowName,
            string status,
            string conclusion,
            DateTime createdAt,
            string workflowEvent)
        {
            return new GITActivityModel
            {
                WorkflowName = workflowName,
                Status = Enum.TryParse<WorkflowStatus>(Normalize(status), true, out var s) ? s : WorkflowStatus.Unknown,
                Conclusion = Enum.TryParse<WorkflowConclusion>(Normalize(conclusion), true, out var c) ? c : WorkflowConclusion.Unknown,
                CreatedAt = createdAt,
                Event = Enum.TryParse<WorkflowEvent>(Normalize(workflowEvent), true, out var e) ? e : WorkflowEvent.Unknown
            };
        }

        private static string Normalize(string? value)
        {
            var normalized = value?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant() ?? "";
            // Map the US spelling to the single canonical enum value
            if (normalized == "canceled") normalized = "cancelled";
            return normalized;
        }
    }
}
