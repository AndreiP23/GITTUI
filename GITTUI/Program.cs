using DotNetEnv;
using GITTUI.Services;
using GITTUI.Views;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

// 1. Parse CLI flags
var useMock = args.Any(a => a.Equals("--mock", StringComparison.OrdinalIgnoreCase));
var mockDelayMs = ResolveMockDelayMs(args) ?? 50;

// Token only required when hitting the real GitHub API
var token = ResolveToken(args);

if (!useMock && string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("No GitHub token provided.");
    Console.Error.WriteLine("Usage:  GITTUI --token <your-token>");
    Console.Error.WriteLine("   or:  set GITHUB_TOKEN=<your-token> then run GITTUI");
    Console.Error.WriteLine("   or:  create a .env file with GITHUB_TOKEN=<your-token>");
    Console.Error.WriteLine("   or:  GITTUI --mock [--mock-delay <ms>]   (run with deterministic in-memory data)");
    return 1;
}

// 2. Build the Generic Host (config, logging, DI, background services)
var host = Host.CreateDefaultBuilder(Array.Empty<string>())
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureServices((ctx, services) =>
    {
        // Strongly-typed option sections
        services.Configure<GitHubOptions>(ctx.Configuration.GetSection(GitHubOptions.Section));
        services.Configure<CacheOptions>(ctx.Configuration.GetSection(CacheOptions.Section));
        services.Configure<AutoRefreshOptions>(ctx.Configuration.GetSection(AutoRefreshOptions.Section));

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<MetricsService>();
        services.AddSingleton<MetricsExportService>();
        services.AddSingleton<RuntimeSettingsService>();

        // GitHub service: real or mock implementation wrapped by a caching decorator
        services.AddSingleton<CachingGitHubService>(s =>
        {
            IGitHubService inner = useMock
                ? new MockGitHubService(
                    s.GetRequiredService<MetricsService>(),
                    TimeSpan.FromMilliseconds(mockDelayMs))
                : new GitHubService(
                    token!,
                    s.GetRequiredService<ILogger<GitHubService>>(),
                    s.GetRequiredService<IOptions<GitHubOptions>>(),
                    s.GetRequiredService<MetricsService>());

            return new CachingGitHubService(
                inner,
                s.GetRequiredService<IMemoryCache>(),
                s.GetRequiredService<ILogger<CachingGitHubService>>(),
                s.GetRequiredService<MetricsService>(),
                s.GetRequiredService<RuntimeSettingsService>());
        });

        // Both IGitHubService and ICacheInvalidator resolve to the same singleton decorator
        services.AddSingleton<IGitHubService>(s => s.GetRequiredService<CachingGitHubService>());
        services.AddSingleton<ICacheInvalidator>(s => s.GetRequiredService<CachingGitHubService>());

        services.AddSingleton<TaskProcessorFactory>();
        services.AddSingleton<ExperimentRunner>();

        // AutoRefreshService registered as both a singleton (for injection into MainView)
        // and a hosted service so the generic host manages its lifetime.
        services.AddSingleton<AutoRefreshService>();
        services.AddHostedService(s => s.GetRequiredService<AutoRefreshService>());

        services.AddSingleton<MainView>();
    })
    .Build();

// 3. Start background services, then run the TUI on the main thread
await host.StartAsync();

var mainView = host.Services.GetRequiredService<MainView>();
mainView.Run();

await host.StopAsync();
return 0;

static string? ResolveToken(string[] args)
{
    // Priority 1: --token command-line argument
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Equals("--token", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 < args.Length)
                return args[i + 1];
            Console.Error.WriteLine("Error: --token requires a value.");
            return null;
        }
    }

    // Priority 2: existing GITHUB_TOKEN environment variable
    var envToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (!string.IsNullOrWhiteSpace(envToken))
        return envToken;

    // Priority 3: .env file in current directory
    if (File.Exists(".env"))
    {
        Env.Load();
        return Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }

    return null;
}

static int? ResolveMockDelayMs(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals("--mock-delay", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(args[i + 1], out var ms)
            && ms >= 0)
        {
            return ms;
        }
    }
    return null;
}