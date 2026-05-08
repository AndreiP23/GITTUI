using Microsoft.Extensions.Logging;
using Terminal.Gui;

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
                _repoSelectionCts?.Dispose();
                _repoSelectionCts = new CancellationTokenSource();
                cts = _repoSelectionCts;
            }

            try
            {
                // Debounce: wait before firing API calls
                await Task.Delay(_githubOptions.DebounceDelayMs, cts.Token);

                // Parallel loading: fetch activity + history at the same time
                var activityTask = _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
                var historyTask = _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name, _githubOptions.HistoryDays);

                await Task.WhenAll(activityTask, historyTask);

                cts.Token.ThrowIfCancellationRequested();

                var activities = activityTask.Result;
                var history = historyTask.Result;

                // Atomic swap — safe for concurrent readers
                _currentActivities = activities;

                Application.MainLoop.Invoke(() =>
                {
                    UpdateActivityTable(activities);
                    UpdateGraphTable(history);
                });
            }
            catch (OperationCanceledException)
            {
                // User moved to another repo before debounce finished — expected
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
        }
    }
}
