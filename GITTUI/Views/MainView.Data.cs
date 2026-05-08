using GITTUI.Components;
using GITTUI.Models;
using GITTUI.Services;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private async void RefreshAllData()
        {
            _cacheInvalidator.InvalidateAll();

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
                    _logger.LogError(ex, "[LoadReposAsync] Failed to load repositories");
                    Application.MainLoop.Invoke(() =>
                        MessageBox.ErrorQuery("Error", "Could not load repositories. Check your token and network.", "Ok"));
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
                    RunDetailDialog.Show(activity.WorkflowName, jobs, onRerunFailedJobs: () =>
                    {
                        RerunFailedJobs(repo.Owner, repo.Name, activity.RunId);
                    });
                    _repoStatusItem.Title = $"Selected: {repo.Name}";
                    _statusBar.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShowRunDetails] Failed to load run details for run {RunId}", activity.RunId);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = "Failed to load run details";
                    _statusBar.SetNeedsDisplay();
                });
            }
        }

        private async void RerunFailedJobs(string owner, string repoName, long runId)
        {
            _repoStatusItem!.Title = "Rerunning failed jobs...";
            _statusBar!.SetNeedsDisplay();

            try
            {
                await _gitHubService.RerunFailedJobsAsync(owner, repoName, runId);

                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = "Rerun triggered successfully";
                    _statusBar.SetNeedsDisplay();
                });

                var activities = await _gitHubService.GetRepositoryActivityAsync(owner, repoName);
                // Atomic swap for thread safety
                _currentActivities = activities;
                UpdateActivityTable(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RerunFailedJobs] Failed to rerun jobs for run {RunId}", runId);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = "Rerun failed";
                    _statusBar.SetNeedsDisplay();
                });
            }
        }

        private void OpenCreateWorkflowDialog()
        {
            if (_selectedRepo == null)
            {
                MessageBox.ErrorQuery("Error", "Select a repository first.", "Ok");
                return;
            }

            var repo = _selectedRepo;

            CreateWorkflowDialog.Show(repo.Name, (fileName, yamlContent) =>
            {
                CommitWorkflowFile(repo.Owner, repo.Name, fileName, yamlContent);
            });
        }

        private async void CommitWorkflowFile(string owner, string repoName, string fileName, string yamlContent)
        {
            _repoStatusItem!.Title = $"Committing {fileName}...";
            _statusBar!.SetNeedsDisplay();

            try
            {
                var commitMessage = $"Create workflow {fileName}";
                await _gitHubService.CreateWorkflowFileAsync(owner, repoName, fileName, yamlContent, commitMessage);

                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = $"Workflow {fileName} created successfully";
                    _statusBar.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommitWorkflowFile] Failed to create workflow {FileName}", fileName);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = "Failed to create workflow";
                    _statusBar.SetNeedsDisplay();
                });
            }
        }

        private async void RunBenchmark()
        {
            if (_selectedRepo == null)
            {
                MessageBox.ErrorQuery("Error", "Select a repository first.", "Ok");
                return;
            }

            var repo = _selectedRepo;

            _repoStatusItem!.Title = "Running benchmark (3 passes)...";
            _statusBar!.SetNeedsDisplay();

            try
            {
                var benchmark = new TaskBenchmark(_processorFactory);
                var result = await benchmark.RunAsync(async () =>
                {
                    await _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
                });

                Application.MainLoop.Invoke(() =>
                {
                    BenchmarkDialog.Show(result);
                    _repoStatusItem.Title = $"Selected: {repo.Name}";
                    _statusBar.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RunBenchmark] Benchmark failed for {RepoName}", repo.Name);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem.Title = "Benchmark failed";
                    _statusBar.SetNeedsDisplay();
                });
            }
        }
    }
}
