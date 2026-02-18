using GITTUI.Services;
using Terminal.Gui;

namespace GITTUI.Views
{
    internal class MainView
    {
        private readonly GitHubService _gitHubService;

        public MainView(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public void Run()
        {
            Application.Init();

            var top = Application.Top;
            var win = new Window("GitHub Actions Monitor")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // UI Components
            var repoList = new ListView()
            {
                Width = Dim.Percent(40),
                Height = Dim.Fill()
            };

            var btnRefresh = new Button("Refresh") { X = Pos.Right(repoList) + 1 };

            // Event Handling
            btnRefresh.Clicked += async () =>
            {
                var repos = await _gitHubService.GetRepositoriesAsync();
                Application.MainLoop.Invoke(() =>
                {
                    repoList.SetSource(repos.Select(r => r.Name).ToList());
                });
            };

            win.Add(repoList, btnRefresh);
            top.Add(win);

            Application.Run();
            Application.Shutdown();
        }
    }
}
