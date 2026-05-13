using GITTUI.Components;
using GITTUI.Models;
using GITTUI.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Terminal.Gui;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private void SetStatus(string title)
        {
            Application.MainLoop.Invoke(() =>
            {
                _repoStatusItem!.Title = title;
                RefreshStatusIndicators();
                _statusBar!.SetNeedsDisplay();
            });
        }

        private string BuildSettingsSummary()
        {
            var settings = _runtimeSettings.GetSnapshot();
            var autoRefresh = settings.AutoRefreshEnabled ? $"AR:{settings.AutoRefreshIntervalSeconds}s" : "AR:off";
            return $"H:{settings.HistoryDays} D:{settings.DebounceDelayMs}ms {autoRefresh}";
        }

        private string BuildMetricsSummary()
        {
            var snapshot = _metrics.GetSnapshot();
            return $"API:{snapshot.GitHubApiCallCount} Cache:{snapshot.CacheHitRate:P0}";
        }

        private void RefreshStatusIndicators()
        {
            _processorStatusItem!.Title = $"Processor: {_processorFactory.CurrentTaskType}";
            _settingsStatusItem!.Title = BuildSettingsSummary();
            _metricsStatusItem!.Title = BuildMetricsSummary();
        }

        private async void RefreshAllData(bool isAutomatic = false)
        {
            if (!isAutomatic)
                _metrics.RecordManualRefresh();

            _cacheInvalidator.InvalidateAll();

            SetStatus("Refreshing data...");

            await LoadReposAsync();

            SetStatus("Refresh Complete");
        }

        private Task LoadReposAsync()
        {
            var processor = _processorFactory.GetCurrentProcessor();

            return processor.ProcessAsync(async () =>
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var repos = await _gitHubService.GetRepositoriesAsync();
                    stopwatch.Stop();
                    _metrics.RecordRepositoryLoad(stopwatch.Elapsed);

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

            SetStatus("Loading run details...");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var jobs = await _gitHubService.GetWorkflowRunJobsAsync(repo.Owner, repo.Name, activity.RunId);
                stopwatch.Stop();
                _metrics.RecordRunDetailsLoad(stopwatch.Elapsed);

                Application.MainLoop.Invoke(() =>
                {
                    RunDetailDialog.Show(activity.WorkflowName, jobs, onRerunFailedJobs: () =>
                    {
                        RerunFailedJobs(repo.Owner, repo.Name, activity.RunId);
                    });
                    _repoStatusItem!.Title = $"Selected: {repo.Name}";
                    _statusBar!.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShowRunDetails] Failed to load run details for run {RunId}", activity.RunId);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = "Failed to load run details";
                    _statusBar!.SetNeedsDisplay();
                });
            }
        }

        private async void RerunFailedJobs(string owner, string repoName, long runId)
        {
            SetStatus("Rerunning failed jobs...");

            try
            {
                await _gitHubService.RerunFailedJobsAsync(owner, repoName, runId);

                var settings = _runtimeSettings.GetSnapshot();
                var activityTask = _gitHubService.GetRepositoryActivityAsync(owner, repoName);
                var historyTask = _gitHubService.GetRepositoryActivityAsync(owner, repoName, settings.HistoryDays);
                await Task.WhenAll(activityTask, historyTask);

                var activities = activityTask.Result;
                var history = historyTask.Result;

                // Atomic swap for thread safety
                _currentActivities = activities;
                UpdateActivityTable(activities);
                UpdateGraphTable(history);
                SetStatus("Rerun triggered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RerunFailedJobs] Failed to rerun jobs for run {RunId}", runId);
                SetStatus("Rerun failed");
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
            SetStatus($"Committing {fileName}...");

            try
            {
                var commitMessage = $"Create workflow {fileName}";
                await _gitHubService.CreateWorkflowFileAsync(owner, repoName, fileName, yamlContent, commitMessage);

                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = $"Workflow {fileName} created successfully";
                    _statusBar!.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommitWorkflowFile] Failed to create workflow {FileName}", fileName);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = "Failed to create workflow";
                    _statusBar!.SetNeedsDisplay();
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

            SetStatus("Running benchmark (warmup + 30 passes per processor)...");

            try
            {
                var benchmark = new TaskBenchmark(_processorFactory, _cacheInvalidator);
                var result = await benchmark.RunAsync(async () =>
                {
                    await _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
                });

                Application.MainLoop.Invoke(() =>
                {
                    BenchmarkDialog.Show(result);
                    _repoStatusItem!.Title = $"Selected: {repo.Name}";
                    _statusBar!.SetNeedsDisplay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RunBenchmark] Benchmark failed for {RepoName}", repo.Name);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = "Benchmark failed";
                    _statusBar!.SetNeedsDisplay();
                });
            }
        }

        private async void RunExperimentMatrix()
        {
            if (_selectedRepo == null)
            {
                MessageBox.ErrorQuery("Error", "Select a repository first.", "Ok");
                return;
            }

            var repo = _selectedRepo;
            SetStatus("Running experiment matrix... (this may take several minutes)");

            var progress = new Progress<ExperimentProgress>(p =>
                SetStatus($"Experiment {p.CellIndex}/{p.TotalCells}: {p.Processor} debounce={p.DebounceMs}ms cache={p.CacheState}"));

            try
            {
                var report = await _experimentRunner.RunAsync(repo, progress: progress);

                Application.MainLoop.Invoke(() =>
                {
                    ExperimentResultsDialog.Show(report);
                    SetStatus($"Experiment complete: {report.CsvPath}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RunExperimentMatrix] Experiment failed for {RepoName}", repo.Name);
                SetStatus("Experiment failed");
            }
        }

        private void OpenMetricsDialog()
        {
            MetricsDialog.Show(
                snapshotProvider: () => _metrics.GetSnapshot(),
                settingsProvider: () => _runtimeSettings.GetSnapshot(),
                taskTypeProvider: () => _processorFactory.CurrentTaskType,
                onReset: () =>
                {
                    _metrics.Reset();
                    Application.MainLoop.Invoke(() =>
                    {
                        RefreshStatusIndicators();
                        _repoStatusItem!.Title = "Metrics reset";
                        _statusBar!.SetNeedsDisplay();
                    });
                },
                exportJson: () =>
                {
                    var path = _metricsExportService.ExportJson(
                        _metrics.GetSnapshot(),
                        _runtimeSettings.GetSnapshot(),
                        _processorFactory.CurrentTaskType);

                    Application.MainLoop.Invoke(() =>
                    {
                        RefreshStatusIndicators();
                        _repoStatusItem!.Title = "Metrics exported (JSON)";
                        _statusBar!.SetNeedsDisplay();
                    });

                    return path;
                },
                exportCsv: () =>
                {
                    var path = _metricsExportService.ExportCsv(
                        _metrics.GetSnapshot(),
                        _runtimeSettings.GetSnapshot(),
                        _processorFactory.CurrentTaskType);

                    Application.MainLoop.Invoke(() =>
                    {
                        RefreshStatusIndicators();
                        _repoStatusItem!.Title = "Metrics exported (CSV)";
                        _statusBar!.SetNeedsDisplay();
                    });

                    return path;
                });
        }

        private void OpenSettingsDialog()
        {
            SettingsDialog.Show(_runtimeSettings.GetSnapshot(), settings =>
            {
                _runtimeSettings.Update(settings);
                _cacheInvalidator.InvalidateAll();
                SetStatus("Runtime settings updated");
            });
        }

        private void SetTaskProcessor(TaskType taskType)
        {
            _processorFactory.SetCurrentTaskType(taskType);

            Application.MainLoop.Invoke(() =>
            {
                _repoStatusItem!.Title = $"Processor changed to {taskType}";
                RefreshStatusIndicators();
                _statusBar!.SetNeedsDisplay();
            });
        }
    }
}
