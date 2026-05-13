using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Terminal.Gui;
using GITTUI.Models;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private void WireEventHandlers()
        {
            _repoTable!.SelectedCellChanged += (args) =>
            {
                if (_allRepositories.Count == 0) return;

                int rowIndex = _repoTable.SelectedRow;
                if (rowIndex < 0 || rowIndex >= _allRepositories.Count) return;

                var selectedRepo = _allRepositories[rowIndex];
                _selectedRepo = selectedRepo;

                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = $"Selected: {selectedRepo.Name}";
                    _statusBar!.SetNeedsDisplay();
                });

                // Cancel any previous in-flight request and debounce
                DebouncedLoadRepoData(selectedRepo);
            };

            _activityTable!.CellActivated += (args) =>
            {
                int rowIndex = args.Row;
                if (rowIndex < 0 || rowIndex >= _currentActivities.Count || _selectedRepo == null) return;
                ShowRunDetails(rowIndex);
            };

            _activityTable.KeyPress += (args) =>
            {
                if (args.KeyEvent.Key == Key.Enter || args.KeyEvent.Key == Key.L || args.KeyEvent.Key == (Key.L | Key.ShiftMask))
                {
                    int rowIndex = _activityTable.SelectedRow;
                    if (rowIndex < 0 || rowIndex >= _currentActivities.Count || _selectedRepo == null) return;
                    ShowRunDetails(rowIndex);
                    args.Handled = true;
                }
            };
        }

        private async void DebouncedLoadRepoData(Models.GITRepositoryModel repo)
        {
            // Cancel any previous pending request
            CancellationTokenSource cts;
            lock (_debouncelock)
            {
                _repoSelectionCts?.Cancel();
                _repoSelectionCts = new CancellationTokenSource();
                cts = _repoSelectionCts;
            }

            var token = cts.Token;
            var startedLoading = false;
            var uiLatencyWatch = Stopwatch.StartNew();

            try
            {
                var settings = _runtimeSettings.GetSnapshot();

                // Debounce: wait before firing API calls
                await Task.Delay(settings.DebounceDelayMs, token);

                // Only allow one repo-data load at a time so quick navigation
                // doesn't fan out multiple overlapping API calls.
                await _repoDataLoadGate.WaitAsync(token);

                try
                {
                    token.ThrowIfCancellationRequested();
                    startedLoading = true;

                    // Time from user action to gate acquisition (debounce + queue wait)
                    _metrics.RecordDebounceWait(uiLatencyWatch.Elapsed);

                    var loadWatch = Stopwatch.StartNew();
                    var processor = _processorFactory.GetCurrentProcessor();

                    List<GITActivityModel>? activities = null;
                    List<GITActivityModel>? history = null;

                    await processor.ProcessAsync(async cancellationToken =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Parallel loading: fetch activity + history at the same time
                        var activityTask = _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
                        var historyTask = _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name, settings.HistoryDays);

                        await Task.WhenAll(activityTask, historyTask);

                        activities = activityTask.Result;
                        history = historyTask.Result;
                    }, token);

                    loadWatch.Stop();
                    _metrics.RecordActivityLoad(loadWatch.Elapsed);

                    token.ThrowIfCancellationRequested();

                    // Atomic swap — safe for concurrent readers
                    _currentActivities = activities!;

                    UpdateActivityTable(activities!);
                    UpdateGraphTable(history!);

                    uiLatencyWatch.Stop();
                    _metrics.RecordUiLatency(uiLatencyWatch.Elapsed);
                }
                finally
                {
                    _repoDataLoadGate.Release();
                }
            }
            catch (OperationCanceledException)
            {
                if (startedLoading)
                    _metrics.RecordStaleSelection();
                else
                    _metrics.RecordCancelledSelection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DebouncedLoadRepoData] API error for {RepoName}", repo.Name);
                Application.MainLoop.Invoke(() =>
                {
                    _repoStatusItem!.Title = "Failed to load repository data";
                    _statusBar!.SetNeedsDisplay();
                });
            }
            finally
            {
                lock (_debouncelock)
                {
                    if (ReferenceEquals(_repoSelectionCts, cts))
                        _repoSelectionCts = null;
                }

                cts.Dispose();
            }
        }
    }
}
