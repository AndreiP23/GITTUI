using DotNetEnv;
using GITTUI.Services;
using GITTUI.Views;
using Microsoft.Extensions.DependencyInjection;

// 1. Load Secrets
Env.Load();
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "";

// 2. Setup Dependency Injection
var serviceProvider = new ServiceCollection()
    .AddSingleton(s => new GitHubService(token))
    .AddSingleton<MainView>()
    .BuildServiceProvider();

// 3. Start the Application
var mainView = serviceProvider.GetRequiredService<MainView>();
mainView.Run();