using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GITTUI.Services
{
    internal sealed class AutoRefreshService : BackgroundService
    {
        private readonly ILogger<AutoRefreshService> _logger;
        private readonly MetricsService _metrics;
        private readonly RuntimeSettingsService _runtimeSettings;
        private Action? _refreshCallback;

        public AutoRefreshService(ILogger<AutoRefreshService> logger, RuntimeSettingsService runtimeSettings, MetricsService metrics)
        {
            _logger = logger;
            _runtimeSettings = runtimeSettings;
            _metrics = metrics;
        }

        /// <summary>
        /// Called by MainView once the TUI is ready to receive refresh events.
        /// </summary>
        public void RegisterRefreshCallback(Action callback) => _refreshCallback = callback;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto-refresh service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var settings = _runtimeSettings.GetSnapshot();
                    if (!settings.AutoRefreshEnabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(settings.AutoRefreshIntervalSeconds), stoppingToken);
                    _logger.LogInformation("Auto-refresh triggered");
                    _metrics.RecordAutoRefresh();
                    _refreshCallback?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Auto-refresh stopped");
        }
    }
}
