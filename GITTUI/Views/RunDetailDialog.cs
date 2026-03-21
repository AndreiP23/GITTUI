using Octokit;
using System.Text;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using Application = Terminal.Gui.Application;

namespace GITTUI.Views
{
    internal static class RunDetailDialog
    {
        public static void Show(string workflowName, IReadOnlyList<WorkflowJob> jobs)
        {
            var title = Sanitize(workflowName);
            var dialog = new Dialog($" {title} ", 80, 24)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var textView = new TextView
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill(1),
                Height = Dim.Fill(2),
                ReadOnly = true,
                CanFocus = true,
                WordWrap = false,
                Text = BuildDetailText(jobs)
            };

            var closeButton = new Button("Close", true);
            closeButton.Clicked += () => Application.RequestStop();

            dialog.Add(textView);
            dialog.AddButton(closeButton);

            Application.Run(dialog);
        }

        private static string BuildDetailText(IReadOnlyList<WorkflowJob> jobs)
        {
            var sb = new StringBuilder();

            if (jobs.Count == 0)
            {
                sb.AppendLine("No jobs found for this run.");
                return sb.ToString();
            }

            foreach (var job in jobs)
            {
                var jobIcon = GetConclusionIcon(job.Conclusion?.StringValue);
                var duration = job.CompletedAt.HasValue && job.StartedAt != default
                    ? (job.CompletedAt.Value - job.StartedAt).ToString(@"mm\:ss")
                    : "--:--";

                sb.AppendLine($"{jobIcon}  {Sanitize(job.Name)}  ({job.Status.StringValue})  {duration}");
                sb.AppendLine(new string('-', 60));

                if (job.Steps != null)
                {
                    foreach (var step in job.Steps)
                    {
                        var stepIcon = GetConclusionIcon(step.Conclusion?.StringValue);
                        var stepDuration = step.CompletedAt.HasValue && step.StartedAt.HasValue
                            ? (step.CompletedAt.Value - step.StartedAt.Value).ToString(@"mm\:ss")
                            : "--:--";

                        sb.AppendLine($"   {stepIcon}  {Sanitize(step.Name)}  [{stepDuration}]");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GetConclusionIcon(string? conclusion)
        {
            return (conclusion?.ToLowerInvariant()) switch
            {
                "success" => "[/]",
                "failure" => "[X]",
                "cancelled" or "canceled" => "[-]",
                "skipped" => "[>]",
                "timed_out" => "[!]",
                _ => "[~]",
            };
        }

        private static string Sanitize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                sb.Append(c <= '~' && c >= ' ' ? c : '?');
            }
            return sb.ToString();
        }
    }
}
