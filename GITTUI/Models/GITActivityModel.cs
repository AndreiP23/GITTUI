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
        Canceled,
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

        private static string Normalize(string? value) => value?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant() ?? "";
    }

    internal static class StatusIconMapper
    {
        private static readonly Dictionary<WorkflowStatus, string> ActiveIcons = new()
            {
                { WorkflowStatus.Queued, char.ConvertFromUtf32(0x231B) },
                { WorkflowStatus.Pending, char.ConvertFromUtf32(0x231B) },
                { WorkflowStatus.Waiting, char.ConvertFromUtf32(0x231B) },
                { WorkflowStatus.InProgress, char.ConvertFromUtf32(0x23F3) },
                { WorkflowStatus.Unknown, char.ConvertFromUtf32(0x231B) }
            };

        private static readonly Dictionary<WorkflowConclusion, string> CompletedIcons = new()
            {
                { WorkflowConclusion.Success, char.ConvertFromUtf32(0x2705) },
                { WorkflowConclusion.Failure, char.ConvertFromUtf32(0x274C) },
                { WorkflowConclusion.StartupFailure, char.ConvertFromUtf32(0x274C) },
                { WorkflowConclusion.Cancelled, char.ConvertFromUtf32(0x2612) },
                { WorkflowConclusion.Canceled, char.ConvertFromUtf32(0x2612) },
                { WorkflowConclusion.Aborted, char.ConvertFromUtf32(0x2612) },
                { WorkflowConclusion.Skipped, char.ConvertFromUtf32(0x21AA) },
                { WorkflowConclusion.TimedOut, char.ConvertFromUtf32(0x23F0) },
                { WorkflowConclusion.Unknown, char.ConvertFromUtf32(0x2022) }
            };

        public static string GetIcon(WorkflowStatus status, WorkflowConclusion conclusion)
        {
            if (status != WorkflowStatus.Completed)
            {
                return ActiveIcons.TryGetValue(status, out var activeIcon) ? activeIcon : char.ConvertFromUtf32(0x231B);
            }

            return CompletedIcons.TryGetValue(conclusion, out var completedIcon) ? completedIcon : char.ConvertFromUtf32(0x2022);
        }
    }
}
