using Terminal.Gui;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private void WireEventHandlers()
        {
            _repoTable!.SelectedCellChanged += async (args) =>
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

                try
                {
                    var activities = await _gitHubService.GetRepositoryActivityAsync(selectedRepo.Owner, selectedRepo.Name);
                    _currentActivities = activities;
                    Application.MainLoop.Invoke(() => UpdateActivityTable(activities));

                    var history = await _gitHubService.GetRepositoryActivityAsync(selectedRepo.Owner, selectedRepo.Name, 14);
                    Application.MainLoop.Invoke(() => UpdateGraphTable(history));
                }
                catch { }
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
    }
}
