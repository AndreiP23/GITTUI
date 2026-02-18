using GITTUI.Models;
using GITTUI.Services;
using System.Data;
using System.Management;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal class MainView
    {
        private readonly GitHubService _gitHubService;
        private List<GITRepositoryModel> _allRepositories = new List<GITRepositoryModel>();
        private FrameView repoFrame;
        private TableView repoTable;
        private FrameView activityFrame;
        private TableView activityTable;
        private StatusBar _statusBar;
        private StatusItem _repoStatusItem;

        public MainView(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public void Run()
        {
            Application.Init();

            var blackScheme = new ColorScheme()
            {
                Normal = Attribute.Make(Color.White, Color.Black),
                Focus = Attribute.Make(Color.Brown, Color.Black),
                HotNormal = Attribute.Make(Color.BrightYellow, Color.Black),
                HotFocus = Attribute.Make(Color.BrightYellow, Color.DarkGray),
            };

            repoFrame = new FrameView(" Repositories ")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(40), 
                Height = Dim.Fill(1)
            };
            activityFrame = new FrameView(" Recent Activity ")
            {
                X = Pos.Right(repoFrame),
                Y = 0,
                Width = Dim.Fill(1),
                Height = Dim.Fill(1),
                AutoSize = false
            };

            repoTable = new TableView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                FullRowSelect = false,
            };

            activityTable = new TableView()
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill(1),
                Height = Dim.Fill(),
                FullRowSelect = false
            };
            activityTable.Style.ExpandLastColumn = false;
            activityTable.Style.ShowVerticalCellLines = false;

            repoTable.Style.ShowVerticalCellLines = false;
            repoTable.Style.AlwaysShowHeaders = false;

            repoFrame.Add(repoTable);
            activityFrame.Add(activityTable);

            _repoStatusItem = new StatusItem(Key.Null, "Selected: None", null);
            _statusBar = new StatusBar([
                new StatusItem(Key.CtrlMask | Key.R, "~CTRL-R~ Refresh", () => RefreshAllData()),
                new StatusItem(Key.CtrlMask | Key.Q, "~CTRL-Q~ Quit", () => Application.RequestStop()),
                _repoStatusItem
            ]);

            var menu = new MenuBar([
                new MenuBarItem ("_File", [
                    new MenuItem ("_Refresh", "Get latest data", async () => await LoadReposAsync()),
                    new MenuItem ("_Quit", "Exit Application", () => Application.RequestStop())
                ]),
            ]);

            var win = new Window("GitHub Actions Monitor")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = blackScheme
            };
            win.Add(repoFrame, activityFrame, _statusBar);

            repoTable.SelectedCellChanged += async (args) =>
            {
                if (_allRepositories == null || _allRepositories.Count == 0) return;

                int rowIndex = repoTable.SelectedRow;
                if (rowIndex < 0 || rowIndex >= _allRepositories.Count) return;

                var selectedRepo = _allRepositories[rowIndex];

                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = $"Selected: {selectedRepo.Name}";
                    _statusBar.SetNeedsDisplay();
                });

                try
                {
                    var activities = await _gitHubService.GetRepositoryActivityAsync(selectedRepo.Owner, selectedRepo.Name);
                    Application.MainLoop.Invoke(() => UpdateActivityTable(activities));
                }
                catch { /* Handle transient API errors */ }
            };

            Application.Top.Add(menu, win);

            Task.Run(LoadReposAsync);

            Application.Run();
            Application.Shutdown();
        }
        private async void RefreshAllData()
        {
            _repoStatusItem.Title = "Refreshing data...";
            _statusBar.SetNeedsDisplay();

            await LoadReposAsync();

            _repoStatusItem.Title = "Refresh Complete";
            _statusBar.SetNeedsDisplay();
        }

        private async Task LoadReposAsync()
        {
            try
            {
                var repos = await _gitHubService.GetRepositoriesAsync();

                _allRepositories.Clear();
                _allRepositories.AddRange(repos);

                var dt = new DataTable();
                dt.Columns.Add("Owner", typeof(string));
                dt.Columns.Add("Repository", typeof(string));

                foreach (var r in _allRepositories)
                {
                    dt.Rows.Add(r.Owner, r.Name);
                }

                Application.MainLoop.Invoke(() =>
                {
                    repoTable.Table = dt;
                    repoTable.SelectedRow = 0;

                    ApplyRepoTableStyles();

                    repoTable.SetNeedsDisplay();
                    repoFrame.SetNeedsDisplay();
                    Application.Top.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                Application.MainLoop.Invoke(() =>
                    MessageBox.ErrorQuery("Error", $"Could not load repos: {ex.Message}", "Ok"));
            }
        }

        private void ApplyRepoTableStyles()
        {
            if (repoTable.Table == null || repoTable.Table.Columns.Count < 2) return;

            repoTable.ColorScheme = null;

            var ownerCol = repoTable.Table.Columns[0];
            var repoCol = repoTable.Table.Columns[1];

            var ownerStyle = repoTable.Style.GetOrCreateColumnStyle(ownerCol);
            var repoStyle = repoTable.Style.GetOrCreateColumnStyle(repoCol);

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

        private void UpdateActivityTable(List<GITActivityModel> activities)
        {
            var dt = new DataTable();
            dt.Columns.Add("Stat", typeof(string));
            dt.Columns.Add("Workflow", typeof(string));
            dt.Columns.Add("Event", typeof(string));
            dt.Columns.Add("Date", typeof(string));

            foreach (var act in activities)
            {
                dt.Rows.Add(
                    act.StatusIcon, 
                    act.WorkflowName,
                    act.Event.ToUpper(),
                    act.CreatedAt.ToString("g")
                );
            }

            Application.MainLoop.Invoke(() =>
            {
                activityTable.Table = dt;
                ApplyActivityTableStyles();
                activityTable.SetNeedsDisplay();
            });
        }

        private void ApplyActivityTableStyles()
        {
            if (activityTable.Table == null || activityTable.Table.Columns.Count < 4) return;

            activityTable.Style.ExpandLastColumn = false;

            var colStat = activityTable.Table.Columns["Stat"];
            var colWork = activityTable.Table.Columns["Workflow"];
            var colEvnt = activityTable.Table.Columns["Event"];
            var colDate = activityTable.Table.Columns["Date"];

            // NOT WORKING JUST STUPID BORDER
            activityTable.Style.GetOrCreateColumnStyle(colStat).MinWidth =
            activityTable.Style.GetOrCreateColumnStyle(colStat).MaxWidth = 4;

            activityTable.Style.GetOrCreateColumnStyle(colWork).MinWidth =
            activityTable.Style.GetOrCreateColumnStyle(colWork).MaxWidth = 14;

            activityTable.Style.GetOrCreateColumnStyle(colEvnt).MinWidth =
            activityTable.Style.GetOrCreateColumnStyle(colEvnt).MaxWidth = 10;

            activityTable.Style.GetOrCreateColumnStyle(colDate).MinWidth =
            activityTable.Style.GetOrCreateColumnStyle(colDate).MaxWidth = 16;
        }
    }
}
