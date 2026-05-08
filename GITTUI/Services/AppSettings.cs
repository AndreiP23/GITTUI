namespace GITTUI.Services
{
    internal class GitHubOptions
    {
        public const string Section = "GitHub";
        public int PageSize { get; set; } = 100;
        public int PageCount { get; set; } = 1;
        public int HistoryDays { get; set; } = 14;
        public int DebounceDelayMs { get; set; } = 300;
    }

    internal class CacheOptions
    {
        public const string Section = "Cache";
        public int RepositoriesTtlSeconds { get; set; } = 300;
        public int ActivityTtlSeconds { get; set; } = 60;
    }

    internal class AutoRefreshOptions
    {
        public const string Section = "AutoRefresh";
        public bool Enabled { get; set; } = true;
        public int IntervalSeconds { get; set; } = 120;
    }
}
