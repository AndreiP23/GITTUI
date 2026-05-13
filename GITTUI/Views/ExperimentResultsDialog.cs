using System.Data;
using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class ExperimentResultsDialog
    {
        public static void Show(ExperimentReport report)
        {
            var dialog = new Dialog(" Experiment Results ", 100, 26)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var header = new Label(BuildHeader(report))
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill(1),
                Height = 4
            };

            var table = new TableView
            {
                X = 1,
                Y = 4,
                Width = Dim.Fill(1),
                Height = Dim.Fill(3),
                FullRowSelect = true,
                Table = BuildTable(report)
            };

            var closeButton = new Button("Close", true);
            closeButton.Clicked += () => Application.RequestStop();

            dialog.Add(header, table);
            dialog.AddButton(closeButton);

            Application.Run(dialog);
        }

        private static string BuildHeader(ExperimentReport report)
        {
            var duration = report.CompletedAtUtc - report.StartedAtUtc;
            return
                $"  Repo:     {report.Repo}\n" +
                $"  Duration: {duration.TotalSeconds:F1}s   Cells: {report.Cells.Count}\n" +
                $"  CSV:      {report.CsvPath}";
        }

        private static DataTable BuildTable(ExperimentReport report)
        {
            var dt = new DataTable();
            dt.Columns.Add("Processor", typeof(string));
            dt.Columns.Add("Debounce", typeof(string));
            dt.Columns.Add("Cache", typeof(string));
            dt.Columns.Add("UI p50", typeof(string));
            dt.Columns.Add("UI p95", typeof(string));
            dt.Columns.Add("UI p99", typeof(string));
            dt.Columns.Add("Load p50", typeof(string));
            dt.Columns.Add("Load p95", typeof(string));

            foreach (var c in report.Cells)
            {
                dt.Rows.Add(
                    c.Processor.ToString(),
                    $"{c.DebounceMs}ms",
                    c.CacheState.ToString(),
                    $"{c.UiLatency.P50Ms:F0}ms",
                    $"{c.UiLatency.P95Ms:F0}ms",
                    $"{c.UiLatency.P99Ms:F0}ms",
                    $"{c.ActivityLoad.P50Ms:F0}ms",
                    $"{c.ActivityLoad.P95Ms:F0}ms");
            }
            return dt;
        }
    }
}
