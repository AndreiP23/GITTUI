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
            if (table.Table == null || table.Table.Columns.Count < 5) return;

            table.Style.ExpandLastColumn = false;

            var colStat = table.Table.Columns["Status"];
            var colWork = table.Table.Columns["Workflow"];
            var colEvnt = table.Table.Columns["Event"];
            var colDate = table.Table.Columns["Date"];
            var colLogs = table.Table.Columns["Logs"];

            table.Style.GetOrCreateColumnStyle(colStat).MinWidth =
            table.Style.GetOrCreateColumnStyle(colStat).MaxWidth = 10;

            table.Style.GetOrCreateColumnStyle(colWork).MinWidth =
            table.Style.GetOrCreateColumnStyle(colWork).MaxWidth = 30;

            table.Style.GetOrCreateColumnStyle(colEvnt).MinWidth =
            table.Style.GetOrCreateColumnStyle(colEvnt).MaxWidth = 10;

            table.Style.GetOrCreateColumnStyle(colDate).MinWidth =
            table.Style.GetOrCreateColumnStyle(colDate).MaxWidth = 16;

            table.Style.GetOrCreateColumnStyle(colLogs).MinWidth =
            table.Style.GetOrCreateColumnStyle(colLogs).MaxWidth = 12;

            table.Style.GetOrCreateColumnStyle(colLogs).ColorGetter = (args) =>
            {
                var val = args.CellValue?.ToString();
                if (!string.IsNullOrEmpty(val))
                    return new ColorScheme
                    {
                        Normal = Attribute.Make(Color.BrightCyan, Color.Black),
                        Focus = Attribute.Make(Color.White, Color.DarkGray)
                    };
                return null;
            };
        }

        public static void ApplyGraphTableStyles(TableView table)
        {
            if (table.Table == null || table.Table.Columns.Count < 5) return;

            table.Style.ColumnStyles.Clear();
            table.Style.ExpandLastColumn = false;
            table.FullRowSelect = false;

            var colDate = table.Table.Columns["Date"];
            var colSuccess = table.Table.Columns["Success"];
            var colFailure = table.Table.Columns["Failure"];
            var colCancelled = table.Table.Columns["Cancelled"];
            var colTotal = table.Table.Columns["Total Runs"];

            table.Style.GetOrCreateColumnStyle(colDate).MinWidth =
            table.Style.GetOrCreateColumnStyle(colDate).MaxWidth = 12;

            table.Style.GetOrCreateColumnStyle(colSuccess).MinWidth =
            table.Style.GetOrCreateColumnStyle(colSuccess).MaxWidth = 8;

            table.Style.GetOrCreateColumnStyle(colFailure).MinWidth =
            table.Style.GetOrCreateColumnStyle(colFailure).MaxWidth = 8;

            table.Style.GetOrCreateColumnStyle(colCancelled).MinWidth =
            table.Style.GetOrCreateColumnStyle(colCancelled).MaxWidth = 12;

            table.Style.GetOrCreateColumnStyle(colTotal).MinWidth =
            table.Style.GetOrCreateColumnStyle(colTotal).MaxWidth = 12;

            table.Style.GetOrCreateColumnStyle(colSuccess).ColorGetter = (args) => new ColorScheme
            {
                Normal = Attribute.Make(Color.Green, Color.Black),
                Focus = Attribute.Make(Color.BrightGreen, Color.DarkGray)
            };

            table.Style.GetOrCreateColumnStyle(colFailure).ColorGetter = (args) => new ColorScheme
            {
                Normal = Attribute.Make(Color.Red, Color.Black),
                Focus = Attribute.Make(Color.BrightRed, Color.DarkGray)
            };

            table.Style.GetOrCreateColumnStyle(colCancelled).ColorGetter = (args) => new ColorScheme
            {
                Normal = Attribute.Make(Color.BrightYellow, Color.Black),
                Focus = Attribute.Make(Color.BrightMagenta, Color.DarkGray)
            };
        }
    }
}
