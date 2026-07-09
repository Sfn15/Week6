using Serilog;
using Week6.Services.Email;

namespace Week6.Services.Reports;

public class ReportGeneratorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<ReportGeneratorBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public ReportGeneratorBackgroundService(IServiceScopeFactory scopeFactory, IEmailQueue emailQueue, ILogger<ReportGeneratorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            await GenerateAndDistributeReportAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GenerateAndDistributeReportAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        _logger.LogInformation("Generating scheduled report...");

        

        try {
            var filePath = await GenerateCsvReportAsync(ct); // TODO: real query after ex 4
            await UploadToStorageAsync(filePath, ct);

            await _emailQueue.EnqueueAsync(new EmailMessage(
                Guid.NewGuid(),
                "admin@week6.local",
                EmailTemplate.OrderConfirmation,
                new Dictionary<string, string> { ["reportPath"] = filePath}), ct);

            _logger.LogInformation("Report generated and queued for distribution: {Path}", filePath);
        } catch (Exception e)
        {
            _logger.LogError(e, "Report generation failed");
        }
    }


    private Task<string> GenerateCsvReportAsync(CancellationToken ct)
    {
        var path = Path.Combine(Path.GetTempPath(), $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        File.WriteAllText(path, "OrderId,Total\n"); // stub content
        return Task.FromResult(path);
    }

    private Task UploadToStorageAsync(string filePath, CancellationToken ct)
    {
        _logger.LogDebug("Would upload {Path} to cloud storage here", filePath);
        return Task.CompletedTask;
    }

}