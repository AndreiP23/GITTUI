using Terminal.Gui;
using System;

namespace GITTUI.Components
{
    internal static class MenuBarFactory
    {
        public static MenuBar Create(Func<Task> refreshAction, Action quitAction)
        {
            return new MenuBar(new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Refresh", "Get latest data", async () => await refreshAction()),
                    new MenuItem("_Quit", "Exit Application", quitAction)
                })
            });
        }
    }
}
