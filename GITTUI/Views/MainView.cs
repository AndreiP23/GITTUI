using GITTUI.Models;
using GITTUI.Services;
using GITTUI.Components;
using System.Data;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal class MainView
    {
        private readonly GitHubService _gitHubService;
        private List<GITRepositoryModel> _allRepositories = new List<GITRepositoryModel>();
        private TableViewWithFrame repoFrame;
        private TableView repoTable;
        private TableViewWithFrame activityFrame;
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

            repoFrame = new TableViewWithFrame(" Repositories ", 0, 0, 40, 1);
            repoTable = repoFrame.TableView;

            activityFrame = new TableViewWithFrame(" Recent Activity ", 40, 0, 60, 1);
            activityTable = activityFrame.TableView;

            (_statusBar, _repoStatusItem) = StatusBarFactory.Create(
                refreshAction: () => RefreshAllData(),
                quitAction: () => Application.RequestStop()
            );

            var menu = MenuBarFactory.Create(
                refreshAction: () => LoadReposAsync(),
                quitAction: () => Application.RequestStop()
            );

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

        private void RefreshAllData()
        {
            _repoStatusItem.Title = "Refreshing data...";
            _statusBar.SetNeedsDisplay();

             LoadReposAsync();

            _repoStatusItem.Title = "Refresh Complete";
            _statusBar.SetNeedsDisplay();
        }

        private Task LoadReposAsync()
        {
            var processor = TaskProcessorFactory.GetProcessor(TaskType.Concurrent);

            return processor.ProcessAsync(async () =>
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

                        TableStyleProvider.ApplyRepoTableStyles(repoTable);

                        repoTable.SetNeedsDisplay();
                        repoFrame.SetNeedsDisplay();
                        Application.Top.SetNeedsDisplay();

                        _repoStatusItem.Title = "Repositories loaded";
                        _statusBar.SetNeedsDisplay();
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                        MessageBox.ErrorQuery("Error", $"Could not load repos: {ex.Message}", "Ok"));
                }
            });
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
                    act.Event.ToString().ToUpper(),
                    act.CreatedAt.ToString("g")
                );
            }

            Application.MainLoop.Invoke(() =>
            {
                activityTable.Table = dt;
                TableStyleProvider.ApplyActivityTableStyles(activityTable);
                activityTable.SetNeedsDisplay();
            });
        }
    }
}
