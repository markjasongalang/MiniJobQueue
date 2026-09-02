using System.Threading.Channels;

namespace MiniJobQueue;

public sealed class JobQueue
{
    private readonly Channel<Job> _channel = Channel.CreateUnbounded<Job>();

    // Returns ValueTask to avoid heap allocations since unbounded channel writes complete
    // synchronously
    public ValueTask EnqueueAsync(Job job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public void Complete() => _channel.Writer.Complete();

    // Replaces exposing .Reader directly while keeping async stream benefits
    public IAsyncEnumerable<Job> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
