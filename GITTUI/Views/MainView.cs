using GITTUI.Models;
using GITTUI.Services;
using GITTUI.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ILogger<MainView> _logger;
        private readonly GitHubOptions _githubOptions;
        private List<GITRepositoryModel> _allRepositories = new();
        private List<GITActivityModel> _currentActivities = new();
        private GITRepositoryModel? _selectedRepo;

        private CancellationTokenSource? _repoSelectionCts;
        private readonly object _debouncelock = new();

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

        public MainView(
            IGitHubService gitHubService,
            TaskProcessorFactory processorFactory,
            AutoRefreshService autoRefreshService,
            ICacheInvalidator cacheInvalidator,
            ILogger<MainView> logger,
            IOptions<GitHubOptions> githubOptions)
        {
            _gitHubService = gitHubService;
            _processorFactory = processorFactory;
            _autoRefreshService = autoRefreshService;
            _cacheInvalidator = cacheInvalidator;
            _logger = logger;
            _githubOptions = githubOptions.Value;
        }

        public void Run()
        {
            Application.Init();

            InitializeLayout();
            var win = CreateMainWindow();
            WireEventHandlers();

            Application.Top.Add(_menu, win);

            _autoRefreshService.RegisterRefreshCallback(() => RefreshAllData());

            Task.Run(LoadReposAsync);

            Application.Run();
            Application.Shutdown();
        }
    }
}
