// Services/OrderProcessingQueue.cs
using System.Threading.Channels;

namespace Week6.Services;

// A simple in-memory queue.
public interface IOrderProcessingQueue
{
    ValueTask EnqueueAsync(int orderId, CancellationToken ct = default);
    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct);
}

public class OrderProcessingQueue : IOrderProcessingQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public ValueTask EnqueueAsync(int orderId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(orderId, ct);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}