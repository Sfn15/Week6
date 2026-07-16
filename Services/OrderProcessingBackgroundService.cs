// Services/OrderProcessingBackgroundService.cs
namespace Week6.Services;

public class OrderProcessingBackgroundService : BackgroundService
{
    private readonly IOrderProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessingBackgroundService> _logger;

    public OrderProcessingBackgroundService(
        IOrderProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderProcessingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order processing service started.");

        // This awaits new items as they arrive — no manual polling loop needed.
        await foreach (var orderId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessOrderAsync(orderId, stoppingToken);
            }
            catch (Exception ex)
            {
                // Never let one bad order kill the whole background service.
                _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
            }
        }
    }

    private async Task ProcessOrderAsync(int orderId, CancellationToken ct)
    {
        // Scoped services (Dapper connection, repository, etc.) must be created
        // per-iteration via a scope — the BackgroundService itself is a singleton.
        using var scope = _scopeFactory.CreateScope();

        // Once Exercise 4 gives us an IOrderRepository backed by Dapper, this
        // becomes: var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        _logger.LogInformation("Processing order {OrderId}...", orderId);

        await Task.Delay(500, ct); // placeholder for real DB work

        _logger.LogInformation("Order {OrderId} processed successfully.", orderId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order processing service is stopping.");
        await base.StopAsync(cancellationToken);
    }
}