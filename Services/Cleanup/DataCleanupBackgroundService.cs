namespace Week6.Services.Cleanup;


public class DataCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataCleanupBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public DataCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DataCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        _logger.LogInformation("Starting scheduled data cleanup...");

        var report = new CleanupReport();

        try
        {
            // once we get a DB change these to DB queries
            report.LogEntriesRemoved = await CleanOldLogEntriesAsync(ct);
            report.CacheEntriesCleared = await CleanExpiredCacheAsync(ct);
            report.RecordsArchived = await ArchiveOldUserDataAsync(ct);

            _logger.LogInformation(
                "Cleanup complete: {Logs} logs removed, {Cache} cache entries cleared, {Archived} records archived",
                report.LogEntriesRemoved, report.CacheEntriesCleared, report.RecordsArchived);    
        } catch (Exception e)
        {
            _logger.LogError(e, "Data cleanup run failed");
        }
    }


    private Task<int> CleanOldLogEntriesAsync(CancellationToken ct) => Task.FromResult(0); // TODO: after Exercise 4
    private Task<int> CleanExpiredCacheAsync(CancellationToken ct) => Task.FromResult(0);   // TODO: after Exercise 3
    private Task<int> ArchiveOldUserDataAsync(CancellationToken ct) => Task.FromResult(0);  // TODO: after Exercise 4
}


public class CleanupReport
{
    public int LogEntriesRemoved { get; set; }
    public int CacheEntriesCleared { get; set; }
    public int RecordsArchived { get; set; }
}