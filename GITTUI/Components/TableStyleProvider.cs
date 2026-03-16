using Terminal.Gui;
using System.Data;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Components
{
    internal static class TableStyleProvider
    {
        public static void ApplyRepoTableStyles(TableView table)
        {
            if (table.Table == null || table.Table.Columns.Count < 2) return;

            table.ColorScheme = null;

            var ownerCol = table.Table.Columns[0];
            var repoCol = table.Table.Columns[1];

            var ownerStyle = table.Style.GetOrCreateColumnStyle(ownerCol);
            var repoStyle = table.Style.GetOrCreateColumnStyle(repoCol);

            ownerStyle.MinWidth = 8;
            ownerStyle.MaxWidth = 12;

            repoStyle.MinWidth = 10;
            repoStyle.MaxWidth = 40;

            ownerStyle.ColorGetter = (args) => new ColorScheme
            {
                Normal = Attribute.Make(Color.Gray, Color.Black),
                Focus = Attribute.Make(Color.White, Color.DarkGray)
            };

            repoStyle.ColorGetter = (args) => new ColorScheme
            {
                Normal = Attribute.Make(Color.Brown, Color.Black),
                Focus = Attribute.Make(Color.Black, Color.Red)
            };
        }

        public static void ApplyActivityTableStyles(TableView table)
        {
            if (table.Table == null || table.Table.Columns.Count < 4) return;

            table.Style.ExpandLastColumn = false;

            var colStat = table.Table.Columns["Stat"];
            var colWork = table.Table.Columns["Workflow"];
            var colEvnt = table.Table.Columns["Event"];
            var colDate = table.Table.Columns["Date"];

            table.Style.GetOrCreateColumnStyle(colStat).MinWidth =
            table.Style.GetOrCreateColumnStyle(colStat).MaxWidth = 4;

            table.Style.GetOrCreateColumnStyle(colWork).MinWidth =
            table.Style.GetOrCreateColumnStyle(colWork).MaxWidth = 14;

            table.Style.GetOrCreateColumnStyle(colEvnt).MinWidth =
            table.Style.GetOrCreateColumnStyle(colEvnt).MaxWidth = 10;

            table.Style.GetOrCreateColumnStyle(colDate).MinWidth =
            table.Style.GetOrCreateColumnStyle(colDate).MaxWidth = 16;
        }
    }
}
