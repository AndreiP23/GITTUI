using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class BenchmarkDialog
    {
        public static void Show(BenchmarkResult result)
        {
            var dialog = new Dialog(" Processor Benchmark ", 70, 18)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            static string Fmt(BenchmarkTimings t) =>
                $"Mean {t.Mean,6:F0}ms | Med {t.Median,5}ms | Min {t.Min,5}ms | Max {t.Max,5}ms";

            var text = new Label(
                $"  Lightweight (Task.Run):\n    {Fmt(result.Lightweight)}\n" +
                $"  Concurrent  (Channel):\n    {Fmt(result.Concurrent)}\n" +
                $"  Isolated    (LongRunning):\n    {Fmt(result.Isolated)}\n\n" +
                $"  5 measured runs per processor (1 warmup).\n" +
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
    }
}
