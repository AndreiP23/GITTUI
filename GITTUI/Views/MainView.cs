using GITTUI.Models;
using GITTUI.Services;
using GITTUI.Components;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal class MainView
    {
        private readonly IGitHubService _gitHubService;
        private readonly TaskProcessorFactory _processorFactory;
        private List<GITRepositoryModel> _allRepositories = new();
        private TableViewWithFrame? _repoFrame;
        private TableView? _repoTable;
        private TableViewWithFrame? _activityFrame;
        private TableView? _activityTable;
        private GraphFrameView? _graphFrame;
        private TableView? _graphTable;
        private StatusBar? _statusBar;
        private StatusItem? _repoStatusItem;

        public MainView(IGitHubService gitHubService, TaskProcessorFactory processorFactory)
        {
            _gitHubService = gitHubService;
            _processorFactory = processorFactory;
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

            _repoFrame = new TableViewWithFrame(" Repositories ", 0, 0, 40, 1);
            _repoTable = _repoFrame.TableView;
            _repoFrame.Height = Dim.Percent(60);

            _activityFrame = new TableViewWithFrame(" Recent Activity ", 40, 0, 60, 1);
            _activityTable = _activityFrame.TableView;
            _activityFrame.Height = Dim.Percent(60);

            _graphFrame = new GraphFrameView();
            _graphTable = _graphFrame.TableView;

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
            win.Add(_repoFrame, _activityFrame, _graphFrame, _statusBar);

            _repoTable.SelectedCellChanged += async (args) =>
            {
                if (_allRepositories.Count == 0) return;

                int rowIndex = _repoTable.SelectedRow;
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

                    var history = await _gitHubService.GetRepositoryActivityAsync(selectedRepo.Owner, selectedRepo.Name, 14);
                    Application.MainLoop.Invoke(() => UpdateGraphTable(history));
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
            _repoStatusItem!.Title = "Refreshing data...";
            _statusBar!.SetNeedsDisplay();

            await LoadReposAsync();

            Application.MainLoop.Invoke(() =>
            {
                _repoStatusItem.Title = "Refresh Complete";
                _statusBar.SetNeedsDisplay();
            });
        }

        private Task LoadReposAsync()
        {
            var processor = _processorFactory.GetProcessor(TaskType.Concurrent);

            return processor.ProcessAsync(async () =>
            {
                try
                {
                    var repos = await _gitHubService.GetRepositoriesAsync();

                    _allRepositories.Clear();
                    _allRepositories.AddRange(repos);

                    var dt = DataTableBuilder.BuildRepoTable(_allRepositories);

                    Application.MainLoop.Invoke(() =>
                    {
                        _repoTable!.Table = dt;
                        _repoTable.SelectedRow = 0;

                        TableStyleProvider.ApplyRepoTableStyles(_repoTable);

                        _repoTable.SetNeedsDisplay();
                        _repoFrame!.SetNeedsDisplay();
                        Application.Top.SetNeedsDisplay();

                        _repoStatusItem!.Title = "Repositories loaded";
                        _statusBar!.SetNeedsDisplay();
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
            var dt = DataTableBuilder.BuildActivityTable(activities);

            Application.MainLoop.Invoke(() =>
            {
                _activityTable!.Table = dt;
                TableStyleProvider.ApplyActivityTableStyles(_activityTable);
                _activityTable.Update();
                _activityTable.SetNeedsDisplay();
            });
        }

        /// <summary>
        /// Populates the graph panel with a summary table and a colored bar chart.
        /// </summary>
        private void UpdateGraphTable(List<GITActivityModel> history)
        {
            var (dt, summaries) = DataTableBuilder.BuildSummaryTable(history);

            Application.MainLoop.Invoke(() =>
            {
                _graphTable!.Table = dt;
                TableStyleProvider.ApplyGraphTableStyles(_graphTable);
                _graphTable.Update();
                _graphTable.SetNeedsDisplay();

                _graphFrame!.BarChart.SetData(summaries);
            });
        }
        }
    }
