using GITTUI.Components;
using GITTUI.Models;
using GITTUI.Services;
using Terminal.Gui;

namespace GITTUI.Views
{
    internal partial class MainView
    {
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

        private async void ShowRunDetails(int rowIndex)
        {
            var activity = _currentActivities[rowIndex];
            var repo = _selectedRepo!;

            _repoStatusItem!.Title = "Loading run details...";
            _statusBar!.SetNeedsDisplay();

            try
            {
                var jobs = await _gitHubService.GetWorkflowRunJobsAsync(repo.Owner, repo.Name, activity.RunId);

                Application.MainLoop.Invoke(() =>
                {
                    RunDetailDialog.Show(activity.WorkflowName, jobs);
                    _repoStatusItem.Title = $"Selected: {repo.Name}";
                    _statusBar.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = $"Failed to load details: {ex.Message}";
                    _statusBar.SetNeedsDisplay();
                });
            }
        }
    }
}
