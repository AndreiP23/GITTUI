using Terminal.Gui;

namespace GITTUI.Components
{
    /// <summary>
    /// A framed panel containing a summary TableView (left) and a BarChartView (right).
    /// Positioned below the repo and activity panels.
    /// </summary>
    internal class GraphFrameView : FrameView
    {
        public TableView TableView { get; }
        public BarChartView BarChart { get; }

        public GraphFrameView()
            : base(" Workflow Summary ")
        {
            X = 0;
            Y = Pos.Percent(60);
            Width = Dim.Fill();
            Height = Dim.Fill(1);

            TableView = new TableView
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(40),
                Height = Dim.Fill(),
                FullRowSelect = true
            };

            BarChart = new BarChartView
            {
                X = Pos.Percent(40),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            Add(TableView, BarChart);
        }
    }
}
