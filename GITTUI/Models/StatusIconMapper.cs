namespace GITTUI.Models
{
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
