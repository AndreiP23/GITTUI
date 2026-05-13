using Terminal.Gui;
using System;

namespace GITTUI.Components
{
    internal static class StatusBarFactory
    {
        public static (StatusBar statusBar, StatusItem repoStatusItem, StatusItem processorStatusItem, StatusItem settingsStatusItem, StatusItem metricsStatusItem) Create(
            Action refreshAction,
            Action quitAction,
            string processorName,
            string settingsSummary,
            string metricsSummary)
        {
            var repoStatusItem = new StatusItem(Key.Null, "Selected: None", null);
            var processorStatusItem = new StatusItem(Key.Null, $"Processor: {processorName}", null);
            var settingsStatusItem = new StatusItem(Key.Null, settingsSummary, null);
            var metricsStatusItem = new StatusItem(Key.Null, metricsSummary, null);
            var statusBar = new StatusBar(new[]
            {
                new StatusItem(Key.CtrlMask | Key.R, "~CTRL-R~ Refresh", refreshAction),
                new StatusItem(Key.CtrlMask | Key.Q, "~CTRL-Q~ Quit", quitAction),
                repoStatusItem,
                processorStatusItem,
                settingsStatusItem,
                metricsStatusItem
            });
            return (statusBar, repoStatusItem, processorStatusItem, settingsStatusItem, metricsStatusItem);
        }
    }
}
