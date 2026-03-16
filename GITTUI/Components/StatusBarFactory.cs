using Terminal.Gui;
using System;

namespace GITTUI.Components
{
    internal static class StatusBarFactory
    {
        public static (StatusBar statusBar, StatusItem repoStatusItem) Create(
            Action refreshAction,
            Action quitAction)
        {
            var repoStatusItem = new StatusItem(Key.Null, "Selected: None", null);
            var statusBar = new StatusBar(new[]
            {
                new StatusItem(Key.CtrlMask | Key.R, "~CTRL-R~ Refresh", refreshAction),
                new StatusItem(Key.CtrlMask | Key.Q, "~CTRL-Q~ Quit", quitAction),
                repoStatusItem
            });
            return (statusBar, repoStatusItem);
        }
    }
}
