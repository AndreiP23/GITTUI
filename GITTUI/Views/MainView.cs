using GITTUI.Models;
using GITTUI.Services;
using GITTUI.Components;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private readonly IGitHubService _gitHubService;
        private readonly TaskProcessorFactory _processorFactory;
        private readonly AutoRefreshService _autoRefreshService;
        private readonly ICacheInvalidator _cacheInvalidator;
        private readonly RuntimeSettingsService _runtimeSettings;
        private readonly MetricsService _metrics;
        private readonly MetricsExportService _metricsExportService;
        private readonly ExperimentRunner _experimentRunner;
        private readonly ILogger<MainView> _logger;
        private List<GITRepositoryModel> _allRepositories = new();
        private List<GITActivityModel> _currentActivities = new();
        private GITRepositoryModel? _selectedRepo;

        private CancellationTokenSource? _repoSelectionCts;
        private readonly object _debouncelock = new();
        private readonly SemaphoreSlim _repoDataLoadGate = new(1, 1);

        private ColorScheme? _blackScheme;
        private MenuBar? _menu;
        private TableViewWithFrame? _repoFrame;
        private TableView? _repoTable;
        private TableViewWithFrame? _activityFrame;
        private TableView? _activityTable;
        private GraphFrameView? _graphFrame;
        private TableView? _graphTable;
        private StatusBar? _statusBar;
        private StatusItem? _repoStatusItem;
        private StatusItem? _processorStatusItem;
        private StatusItem? _settingsStatusItem;
        private StatusItem? _metricsStatusItem;

        public MainView(
            IGitHubService gitHubService,
            TaskProcessorFactory processorFactory,
            AutoRefreshService autoRefreshService,
            ICacheInvalidator cacheInvalidator,
            RuntimeSettingsService runtimeSettings,
            MetricsService metrics,
            MetricsExportService metricsExportService,
            ExperimentRunner experimentRunner,
            ILogger<MainView> logger)
        {
            _gitHubService = gitHubService;
            _processorFactory = processorFactory;
            _autoRefreshService = autoRefreshService;
            _cacheInvalidator = cacheInvalidator;
            _runtimeSettings = runtimeSettings;
            _metrics = metrics;
            _metricsExportService = metricsExportService;
            _experimentRunner = experimentRunner;
            _logger = logger;
        }

        public void Run()
        {
            Application.Init();

            InitializeLayout();
            var win = CreateMainWindow();
            WireEventHandlers();

            Application.Top.Add(_menu, win);

            _autoRefreshService.RegisterRefreshCallback(() =>
                Application.MainLoop.Invoke(() => RefreshAllData(isAutomatic: true)));

            Task.Run(LoadReposAsync);

            Application.Run();
            Application.Shutdown();
        }
    }
}
