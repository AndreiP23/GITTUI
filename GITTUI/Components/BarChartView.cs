using GITTUI.Models;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Components
{
    /// <summary>
    /// Custom view that renders horizontal stacked bars per day.
    /// Each row: "MM/dd │██████████████████  " where segments are colored
    /// green (success), red (failure), yellow (cancelled).
    /// </summary>
    internal class BarChartView : View
    {
        private List<WorkflowActivitySummary> _data = new();
        private int _barMaxWidth = 30;

        private static readonly Attribute SuccessAttr = Attribute.Make(Color.Green, Color.Black);
        private static readonly Attribute FailureAttr = Attribute.Make(Color.Red, Color.Black);
        private static readonly Attribute CancelledAttr = Attribute.Make(Color.BrightYellow, Color.Black);
        private static readonly Attribute LabelAttr = Attribute.Make(Color.Gray, Color.Black);
        private static readonly Attribute EmptyAttr = Attribute.Make(Color.DarkGray, Color.Black);

        public void SetData(List<WorkflowActivitySummary> data)
        {
            _data = data;
            SetNeedsDisplay();
        }

        public override void Redraw(Rect bounds)
        {
            Clear();

            if (_data.Count == 0) return;

            int maxTotal = _data.Max(d => d.TotalRuns);
            if (maxTotal == 0) maxTotal = 1;

            // Leave space for the label "MM/dd ##  " prefix
            int labelWidth = 10;
            _barMaxWidth = Math.Max(10, bounds.Width - labelWidth - 1);

            for (int row = 0; row < _data.Count && row < bounds.Height; row++)
            {
                var summary = _data[row];

                // Draw date label
                string label = $"{summary.Date:MM/dd}";
                string count = $"{summary.TotalRuns,3}";

                Move(0, row);
                Driver.SetAttribute(LabelAttr);
                foreach (char c in label) Driver.AddRune(c);
                Driver.AddRune(' ');

                Driver.SetAttribute(EmptyAttr);
                foreach (char c in count) Driver.AddRune(c);
                Driver.AddRune(' ');

                // Calculate segment widths proportional to total
                int totalBarWidth = (int)Math.Round((double)summary.TotalRuns / maxTotal * _barMaxWidth);
                totalBarWidth = Math.Max(totalBarWidth, summary.TotalRuns > 0 ? 1 : 0);

                int successWidth = 0, failureWidth = 0, cancelledWidth = 0;
                if (summary.TotalRuns > 0)
                {
                    successWidth = (int)Math.Round((double)summary.SuccessCount / summary.TotalRuns * totalBarWidth);
                    failureWidth = (int)Math.Round((double)summary.FailureCount / summary.TotalRuns * totalBarWidth);
                    cancelledWidth = totalBarWidth - successWidth - failureWidth;
                    if (cancelledWidth < 0)
                    {
                        // Rounding fix: trim from the largest
                        if (successWidth >= failureWidth) successWidth += cancelledWidth;
                        else failureWidth += cancelledWidth;
                        cancelledWidth = 0;
                    }
                }

                // Draw green segment
                Driver.SetAttribute(SuccessAttr);
                for (int i = 0; i < successWidth; i++) Driver.AddRune('█');

                // Draw red segment
                Driver.SetAttribute(FailureAttr);
                for (int i = 0; i < failureWidth; i++) Driver.AddRune('█');

                // Draw yellow segment
                Driver.SetAttribute(CancelledAttr);
                for (int i = 0; i < cancelledWidth; i++) Driver.AddRune('█');

                // Fill remaining with dim dots
                Driver.SetAttribute(EmptyAttr);
                int remaining = _barMaxWidth - totalBarWidth;
                for (int i = 0; i < remaining; i++) Driver.AddRune('░');
            }
        }
    }
}
