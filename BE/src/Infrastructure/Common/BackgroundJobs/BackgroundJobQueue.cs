using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Backend.Infrastructure.Common;

/// <summary>
/// A single unit of queued work plus the token that cancels it. Carried through
/// the channel so the producer (<see cref="InMemoryBackgroundJobService"/>) and
/// the consumer (<see cref="BackgroundJobProcessor"/>) agree on identity and
/// cancellation without sharing anything else.
/// </summary>
public sealed record BackgroundJob(
    string Id,
    string Name,
    Func<IServiceProvider, CancellationToken, Task> Work,
    CancellationToken CancellationToken);

/// <summary>
/// The hand-off point between <see cref="InMemoryBackgroundJobService"/> and
/// <see cref="BackgroundJobProcessor"/>; must be registered as a singleton or
/// the two would each get their own empty channel. Public only because DI needs
/// to construct it - it is an implementation detail of the in-process default
/// and nothing outside Infrastructure should reference it.
/// <para>
/// The channel is unbounded: a bounded one would make <c>EnqueueAsync</c> block
/// the request thread once full, which is exactly what background jobs exist to
/// avoid. The trade-off is that a runaway producer can grow the queue until
/// memory runs out - a durable scheduler (Hangfire/Quartz) is the answer at that
/// point, not back-pressure here.
/// </para>
/// </summary>
public sealed class BackgroundJobQueue
{
    private readonly Channel<BackgroundJob> _channel = Channel.CreateUnbounded<BackgroundJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();

    /// <summary>Registers a cancellation source for <paramref name="jobId"/>, making the job cancellable by id.</summary>
    public CancellationTokenSource Track(string jobId)
    {
        var cancellation = new CancellationTokenSource();
        _cancellations[jobId] = cancellation;

        return cancellation;
    }

    public bool Cancel(string jobId)
    {
        if (!_cancellations.TryGetValue(jobId, out var cancellation)) return false;

        try
        {
            cancellation.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            // The job completed and released its source between the lookup and here.
            return false;
        }
    }

    /// <summary>
    /// Drops the job's cancellation source once it can no longer run. Skipping
    /// this would leak one <see cref="CancellationTokenSource"/> per job for the
    /// lifetime of the process.
    /// </summary>
    public void Release(string jobId)
    {
        if (_cancellations.TryRemove(jobId, out var cancellation)) cancellation.Dispose();
    }

    public ValueTask EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<BackgroundJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
