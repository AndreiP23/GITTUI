using DotNetEnv;
using GITTUI.Services;
using GITTUI.Views;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

// 1. Resolve token: --token arg > GITHUB_TOKEN env var > .env file
var token = ResolveToken(args);

if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("No GitHub token provided.");
    Console.Error.WriteLine("Usage:  GITTUI --token <your-token>");
    Console.Error.WriteLine("   or:  set GITHUB_TOKEN=<your-token> then run GITTUI");
    Console.Error.WriteLine("   or:  create a .env file with GITHUB_TOKEN=<your-token>");
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

        // GitHub service: real implementation wrapped by a caching decorator
        services.AddSingleton<GitHubService>(s => new GitHubService(
            token,
            s.GetRequiredService<ILogger<GitHubService>>(),
            s.GetRequiredService<IOptions<GitHubOptions>>()));

        services.AddSingleton<CachingGitHubService>(s => new CachingGitHubService(
            s.GetRequiredService<GitHubService>(),
            s.GetRequiredService<IMemoryCache>(),
            s.GetRequiredService<ILogger<CachingGitHubService>>(),
            s.GetRequiredService<IOptions<CacheOptions>>()));

        // Both IGitHubService and ICacheInvalidator resolve to the same singleton decorator
        services.AddSingleton<IGitHubService>(s => s.GetRequiredService<CachingGitHubService>());
        services.AddSingleton<ICacheInvalidator>(s => s.GetRequiredService<CachingGitHubService>());

        services.AddSingleton<TaskProcessorFactory>();

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