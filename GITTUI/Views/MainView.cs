using GITTUI.Models;
using GITTUI.Services;
using System.Data;
using Terminal.Gui;

namespace GITTUI.Views
{
    internal class MainView
    {
        private readonly GitHubService _gitHubService;
        private ListView repoList;
        private TableView activityTable;

        public MainView(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public void Run()
        {
            Application.Init();

            var top = Application.Top;
            var win = new Window("GitHub Actions Monitor")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var menu = new MenuBar([
                new MenuBarItem ("_File", [
                    new MenuItem ("_Refresh", "Get latest data", async () => await LoadReposAsync()),
                    new MenuItem ("_Quit", "", () => { Application.RequestStop(); })
                ]),
            ]);

            // 1. Setup Repository List (Left Side)
            repoList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(30),
                Height = Dim.Fill(),
                ColorScheme = Colors.Base
            };

            // 2. Setup Activity Table (Right Side)
            activityTable = new TableView()
            {
                X = Pos.Right(repoList),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // A. Selection Changed (Master-Detail Logic)
            repoList.SelectedItemChanged += async (args) =>
            {
                if (args.Value is GITRepositoryModel selectedRepo)
                {
                    var activities = await _gitHubService.GetRepositoryActivityAsync(selectedRepo.Owner, selectedRepo.Name);
                    Application.MainLoop.Invoke(() =>
                    {
                        UpdateActivityTable(activities);
                    });
                }
            };

            win.Add(repoList, activityTable);
            top.Add(menu, win);

            Application.Run();
            Application.Shutdown();
        }

        private async Task LoadReposAsync()
        {
            var repos = await _gitHubService.GetRepositoriesAsync();
            Application.MainLoop.Invoke(() =>
            {
                // Map the models to strings for the simple ListView
                repoList.SetSource(repos.ToList());
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
                    act.Event.ToUpper(),
                    act.CreatedAt.ToString("g")
                );
            }

            activityTable.Table = dt;
        }
    }
}
