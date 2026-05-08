using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GITTUI.Services
{
    internal sealed class AutoRefreshService : BackgroundService
    {
        private readonly ILogger<AutoRefreshService> _logger;
        private readonly AutoRefreshOptions _options;
        private Action? _refreshCallback;

        public AutoRefreshService(ILogger<AutoRefreshService> logger, IOptions<AutoRefreshOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Called by MainView once the TUI is ready to receive refresh events.
        /// </summary>
        public void RegisterRefreshCallback(Action callback) => _refreshCallback = callback;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Auto-refresh is disabled");
                return;
            }

            _logger.LogInformation("Auto-refresh started. Interval: {Seconds}s", _options.IntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
                    _logger.LogInformation("Auto-refresh triggered");
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
