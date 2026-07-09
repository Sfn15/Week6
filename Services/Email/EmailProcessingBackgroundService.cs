namespace Week6.Services.Email;

public class EmailProcessingBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly ILogger<EmailProcessingBackgroundService> _logger;
    private const int MaxAttempts = 3;

    public EmailProcessingBackgroundService(IEmailQueue queue, ILogger<EmailProcessingBackgroundService> logger)
    {
        _queue = (EmailQueue) queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(message, stoppingToken);
                _queue.TrackStatus(message.Id, message.ToAddress, EmailStatus.Sent);
                _logger.LogInformation("Email {Id} sent to {To} using {Template}",
                    message.Id, message.ToAddress, message.Template);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Email {Id} failed (attempt {Attempt}/{Max})",
                    message.Id, message.ToAddress, message.Template);
                
                if (message.AttemptCount + 1 >= MaxAttempts)
                {
                    _queue.TrackStatus(message.Id, message.ToAddress, EmailStatus.Abandoned, e.Message);
                    _logger.LogError("Email {Id} abandoned after {Max} attempts", message.Id, MaxAttempts);
                } else
                {
                    _queue.TrackStatus(message.Id, message.ToAddress, EmailStatus.Failed, e.Message);
                    _ = DelayedRequeueAsync(message, stoppingToken);
                }
            }
        }
    }

    private async Task DelayedRequeueAsync(EmailMessage message, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5 * (message.AttemptCount + 1)), ct);
        await _queue.RequeueAsync(message, ct);
    }
    private Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var subject = message.Template switch
        {
            EmailTemplate.OrderConfirmation => "Your order has been confirmed",
            EmailTemplate.PasswordReset => "Reset your password",
            EmailTemplate.WelcomeMessage => "Welcome!",
            _ => "Notification"
        };
        _logger.LogDebug("Sending '{Subject}' to {To}", subject, message.ToAddress);
        return Task.Delay(200, ct); // fake network call by waiting
    }
}