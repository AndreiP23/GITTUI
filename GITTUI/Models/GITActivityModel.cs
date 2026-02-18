namespace GITTUI.Models
{
    internal class GITActivityModel
    {
        public required string WorkflowName { get; set; }

        // e.g., "completed", "in_progress"
        public required string Status { get; set; }

        // e.g., "success", "failure"
        public required string Conclusion { get; set; }

        public required DateTime CreatedAt { get; set; }

        // e.g., "push", "pull_request"
        public required string Event { get; set; }

        public string StatusIcon => Conclusion switch
        {
            "success" => "✅",
            "failure" => "❌",
            "cancelled" => "⚪",
            _ => "🕒" // In progress or queued
        };
    }
}
