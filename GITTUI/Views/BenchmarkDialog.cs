using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class BenchmarkDialog
    {
        public static void Show(BenchmarkResult result)
        {
            var dialog = new Dialog(" Processor Benchmark ", 60, 14)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var text = new Label(
                $"  Lightweight (Task.Run):      {result.LightweightMs,6} ms\n" +
                $"  Concurrent  (Channel):       {result.ConcurrentMs,6} ms\n" +
                $"  Isolated    (LongRunning):   {result.IsolatedMs,6} ms\n\n" +
                $"  Each processor ran the same workload (fetch repos).\n" +
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
