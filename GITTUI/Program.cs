using DotNetEnv;
using GITTUI.Services;
using GITTUI.Views;
using Microsoft.Extensions.DependencyInjection;

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

// 2. Setup Dependency Injection
var serviceProvider = new ServiceCollection()
    .AddSingleton<IGitHubService>(s => new GitHubService(token))
    .AddSingleton<TaskProcessorFactory>()
    .AddSingleton<MainView>()
    .BuildServiceProvider();

// 3. Start the Application
var mainView = serviceProvider.GetRequiredService<MainView>();
mainView.Run();
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