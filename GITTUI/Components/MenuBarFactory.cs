using Terminal.Gui;
using System;

namespace GITTUI.Components
{
    internal static class MenuBarFactory
    {
        public static MenuBar Create(
            Func<Task> refreshAction,
            Action quitAction,
            Action? createWorkflowAction = null,
            Action? benchmarkAction = null,
            Action? metricsAction = null,
            Action? settingsAction = null,
            Action? experimentAction = null,
            Action? useSequentialProcessorAction = null,
            Action? useLightweightProcessorAction = null,
            Action? useConcurrentProcessorAction = null,
            Action? useIsolatedProcessorAction = null)
        {
            return new MenuBar(new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Refresh", "Get latest data", async () => await refreshAction()),
                    new MenuItem("_Quit", "Exit Application", quitAction)
                }),
                new MenuBarItem("_Actions", new[]
                {
                    new MenuItem("_Create Workflow", "Create a new workflow file", () => createWorkflowAction?.Invoke()),
                    new MenuItem("_Benchmark Processors", "Compare task processor performance", () => benchmarkAction?.Invoke()),
                    new MenuItem("Run _Experiment Matrix", "Run the full experiment matrix (processor x debounce x cache)", () => experimentAction?.Invoke()),
                    new MenuItem("_Metrics", "Show internal application metrics", () => metricsAction?.Invoke()),
                    new MenuItem("_Settings", "Configure runtime settings", () => settingsAction?.Invoke())
                }),
                new MenuBarItem("_Processor", new[]
                {
                    new MenuItem("Se_quential", "Run synchronously (baseline)", () => useSequentialProcessorAction?.Invoke()),
                    new MenuItem("_Lightweight", "Use Task.Run based processing", () => useLightweightProcessorAction?.Invoke()),
                    new MenuItem("_Concurrent", "Use channel-based processing", () => useConcurrentProcessorAction?.Invoke()),
                    new MenuItem("_Isolated", "Use long-running isolated processing", () => useIsolatedProcessorAction?.Invoke())
                })
            });
        }
    }
}
