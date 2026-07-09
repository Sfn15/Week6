using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Week6.Services.Email;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct);
    void TrackStatus(Guid id, string toAddress, EmailStatus status, string? error = null);
    EmailDeliveryRecord? GetStatus(Guid id);
}


public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    private readonly ConcurrentDictionary<Guid, EmailDeliveryRecord> _statuses = new();

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        TrackStatus(message.Id, message.ToAddress, EmailStatus.Queued);
        return _channel.Writer.WriteAsync(message, ct);
    }

    public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
    
    public void TrackStatus(Guid id, string toAddress, EmailStatus status, string? error = null) =>
        _statuses[id] = new EmailDeliveryRecord(id, toAddress, status, error, DateTime.UtcNow);

    public EmailDeliveryRecord? GetStatus(Guid id) => _statuses.GetValueOrDefault(id);
    
    public ValueTask RequeueAsync(EmailMessage message, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(message with { AttemptCount = message.AttemptCount + 1}, ct);
}