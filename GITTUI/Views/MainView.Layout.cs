using GITTUI.Components;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal partial class MainView
    {
        private void InitializeLayout()
        {
            _blackScheme = new ColorScheme()
            {
                Normal = Attribute.Make(Color.White, Color.Black),
                Focus = Attribute.Make(Color.Brown, Color.Black),
                HotNormal = Attribute.Make(Color.BrightYellow, Color.Black),
                HotFocus = Attribute.Make(Color.BrightYellow, Color.DarkGray),
            };

            _repoFrame = new TableViewWithFrame(" Repositories ", 0, 0, 40, 1);
            _repoTable = _repoFrame.TableView;
            _repoFrame.Height = Dim.Percent(60);

            _activityFrame = new TableViewWithFrame(" Recent Activity ", 40, 0, 60, 1);
            _activityTable = _activityFrame.TableView;
            _activityFrame.Height = Dim.Percent(60);

            _graphFrame = new GraphFrameView();
            _graphTable = _graphFrame.TableView;

            (_statusBar, _repoStatusItem) = StatusBarFactory.Create(
                refreshAction: () => RefreshAllData(),
                quitAction: () => Application.RequestStop()
            );

            _menu = MenuBarFactory.Create(
                refreshAction: () => LoadReposAsync(),
                quitAction: () => Application.RequestStop()
            );
        }

        private Window CreateMainWindow()
        {
            var win = new Window("GitHub Actions Monitor")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = _blackScheme
            };
            win.Add(_repoFrame, _activityFrame, _graphFrame, _statusBar);
            return win;
        }
    }
}
