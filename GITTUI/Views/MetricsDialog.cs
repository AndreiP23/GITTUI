using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class MetricsDialog
    {
        public static void Show(
            Func<MetricsSnapshot> snapshotProvider,
            Func<RuntimeSettings> settingsProvider,
            Func<TaskType> taskTypeProvider,
            Action onReset,
            Func<string> exportJson,
            Func<string> exportCsv)
        {
            var dialog = new Dialog(" Metrics ", 90, 28)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var text = new Label(string.Empty)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(2),
            };

            void RefreshText()
            {
                text.Text = BuildText(snapshotProvider(), settingsProvider(), taskTypeProvider());
                text.SetNeedsDisplay();
            }

            RefreshText();

            var resetButton = new Button("Reset");
            resetButton.Clicked += () =>
            {
                onReset();
                RefreshText();
            };

            var exportJsonButton = new Button("Export JSON");
            exportJsonButton.Clicked += () =>
            {
                var path = exportJson();
                MessageBox.Query("Exported", $"Metrics exported to:\n{path}", "Ok");
                RefreshText();
            };

            var exportCsvButton = new Button("Export CSV");
            exportCsvButton.Clicked += () =>
            {
                var path = exportCsv();
                MessageBox.Query("Exported", $"Metrics exported to:\n{path}", "Ok");
                RefreshText();
            };

            var closeButton = new Button("Close", true);
            closeButton.Clicked += () => Application.RequestStop();

            dialog.Add(text);
            dialog.AddButton(resetButton);
            dialog.AddButton(exportJsonButton);
            dialog.AddButton(exportCsvButton);
            dialog.AddButton(closeButton);

            Application.Run(dialog);
        }

        private static string BuildText(MetricsSnapshot snapshot, RuntimeSettings settings, TaskType currentTaskType)
        {
            string FormatStats(string label, LatencyStats s) =>
                $"  {label,-22} n={s.Count,-4} mean={s.MeanMs,5:F0}ms  sd={s.StdDevMs,5:F0}  p50={s.P50Ms,5:F0}  p95={s.P95Ms,5:F0}  p99={s.P99Ms,5:F0}";

            return
                $"  Processor:             {currentTaskType}\n" +
                $"  History / Debounce:    {settings.HistoryDays}d / {settings.DebounceDelayMs}ms\n" +
                $"  Cache TTLs:            repos {settings.RepositoriesTtlSeconds}s | activity {settings.ActivityTtlSeconds}s\n" +
                $"  Auto Refresh:          {(settings.AutoRefreshEnabled ? "on" : "off")} / {settings.AutoRefreshIntervalSeconds}s\n\n" +
                $"  Manual refreshes:      {snapshot.ManualRefreshCount}\n" +
                $"  Auto refreshes:        {snapshot.AutoRefreshCount}\n" +
                $"  GitHub API calls:      {snapshot.GitHubApiCallCount}\n" +
                $"  Cache hits / misses:   {snapshot.CacheHitCount} / {snapshot.CacheMissCount}  ({snapshot.CacheHitRate:P0})\n" +
                $"  Cancelled / stale:     {snapshot.CancelledSelectionCount} / {snapshot.StaleSelectionCount}\n\n" +
                FormatStats("Repo load:", snapshot.RepositoryLoad) + "\n" +
                FormatStats("Activity load:", snapshot.ActivityLoad) + "\n" +
                FormatStats("Run details load:", snapshot.RunDetailsLoad) + "\n" +
                FormatStats("UI latency:", snapshot.UiLatency) + "\n" +
                FormatStats("Debounce wait:", snapshot.DebounceWait);
        }
    }
}