using GITTUI.Models;
using GITTUI.Services;
using GITTUI.Components;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private readonly IGitHubService _gitHubService;
        private readonly TaskProcessorFactory _processorFactory;
        private List<GITRepositoryModel> _allRepositories = new();
        private List<GITActivityModel> _currentActivities = new();
        private GITRepositoryModel? _selectedRepo;

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

        public MainView(IGitHubService gitHubService, TaskProcessorFactory processorFactory)
        {
            _gitHubService = gitHubService;
            _processorFactory = processorFactory;
        }

        public void Run()
        {
            Application.Init();

            InitializeLayout();
            var win = CreateMainWindow();
            WireEventHandlers();

            Application.Top.Add(_menu, win);

            Task.Run(LoadReposAsync);

            Application.Run();
            Application.Shutdown();
        }
    }
}
