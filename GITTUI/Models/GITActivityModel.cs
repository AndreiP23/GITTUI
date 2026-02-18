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

        public string StatusIcon
        {
            get
            {
                // 1. TRIM and LOWERCASE to kill any hidden formatting issues
                var status = Status?.Trim().ToLower() ?? "";
                var conclusion = Conclusion?.Trim().ToLower() ?? "";

                // --- ACTIVE STATES ---
                // If it's not completed, it's 'In Progress' or 'Queued'
                if (status != "completed")
                {
                    // Use 0x231B (Hourglass) for Queued or 0x23F3 (Running Hourglass) 
                    // These are part of the 'Miscellaneous Symbols' block - very safe.
                    return status switch
                    {
                        "queued" or "pending" or "waiting" => char.ConvertFromUtf32(0x231B),
                        "in_progress" => char.ConvertFromUtf32(0x23F3),
                        _ => char.ConvertFromUtf32(0x231B)
                    };
                }

                // --- COMPLETED STATES ---
                return conclusion switch
                {
                    "success" => char.ConvertFromUtf32(0x2705),
                    "failure" or "startup_failure" => char.ConvertFromUtf32(0x274C),

                    // Cancelled: Use 0x2612 (Square with X) - It is much older/safer than 'No Entry'
                    "cancelled" or "canceled" or "aborted" => char.ConvertFromUtf32(0x2612),

                    "skipped" => char.ConvertFromUtf32(0x21AA), // Simple hooked arrow
                    "timed_out" => char.ConvertFromUtf32(0x23F0), // Alarm clock

                    // If it's completed but we don't recognize the word, 
                    // return a simple bullet (0x2022) so it's not a '?'
                    _ => char.ConvertFromUtf32(0x2022)
                };
            }
        }
    }
}
