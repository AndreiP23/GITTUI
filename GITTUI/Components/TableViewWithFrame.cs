using Terminal.Gui;

namespace GITTUI.Components
{
    internal class TableViewWithFrame : FrameView
    {
        public TableView TableView { get; }

        public TableViewWithFrame(string title, int x, int y, int widthPercent, int heightFill)
              : base(title)
        {
            X = x;
            Y = y;
            Width = Dim.Percent(widthPercent);
            Height = Dim.Fill(heightFill);

            TableView = new TableView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                FullRowSelect = false
            };
            Add(TableView);
        }
    }
}
