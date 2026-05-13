using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class BenchmarkDialog
    {
        public static void Show(BenchmarkResult result)
        {
            var dialog = new Dialog(" Processor Benchmark ", 90, 22)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            static string Row(string label, BenchmarkTimings t) =>
                $"  {label,-22} {t}";

            var text = new Label(
                $"  {_columnHeader}\n" +
                Row("Sequential (baseline):", result.Sequential) + "\n" +
                Row("Lightweight (Task.Run):", result.Lightweight) + "\n" +
                Row("Concurrent  (Channel):", result.Concurrent) + "\n" +
                Row("Isolated    (LongRun):", result.Isolated) + "\n\n" +
                $"  Warmup: {result.WarmupRuns} runs (discarded)\n" +
                $"  Measured: {result.MeasuredRuns} runs per processor\n" +
                $"  Cache invalidated before every pass.\n" +
                $"  Lower is better.")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(2),
            };

            var closeButton = new Button("Close", true);
            closeButton.Clicked += () => Application.RequestStop();

            dialog.Add(text);
            dialog.AddButton(closeButton);

            Application.Run(dialog);
        }

        private const string _columnHeader =
            "Processor              mean      sd  p50  p95  p99  min  max";
    }
}
