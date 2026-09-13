using Backend.Application.Common;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Default <see cref="IBackgroundJobService"/>: an in-process queue drained by
/// <see cref="BackgroundJobProcessor"/>. Enough for "send the welcome e-mail
/// after the response is written" without adding Redis, SQL job tables or a
/// second deployment unit.
/// <para>
/// IMPORTANT CAVEAT: the queue lives in memory only. Anything still queued (or
/// scheduled but not yet due) is silently lost on restart, crash, or a rolling
/// deploy, and there are no retries and no execution history. If a job must
/// survive those, register Hangfire or Quartz against
/// <see cref="IBackgroundJobService"/> instead - callers do not change.
/// </para>
/// </summary>
public sealed class InMemoryBackgroundJobService(BackgroundJobQueue queue) : IBackgroundJobService
{
    public async Task<string> EnqueueAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        string? jobName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        var job = CreateJob(work, jobName);
        await queue.EnqueueAsync(job, cancellationToken);

        return job.Id;
    }

    /// <remarks>
    /// The delay is awaited on a detached task rather than by the processor, so a
    /// job scheduled an hour out never blocks jobs queued behind it.
    /// <paramref name="cancellationToken"/> is intentionally not used to time the
    /// delay: it usually belongs to the HTTP request, which ends long before the
    /// job is due. Use <see cref="CancelAsync"/> with the returned id instead.
    /// </remarks>
    public Task<string> ScheduleAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        TimeSpan delay,
        string? jobName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        var job = CreateJob(work, jobName);
        _ = DelayThenEnqueueAsync(job, delay);

        return Task.FromResult(job.Id);
    }

    public Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        return Task.FromResult(queue.Cancel(jobId));
    }

    private BackgroundJob CreateJob(Func<IServiceProvider, CancellationToken, Task> work, string? jobName)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var cancellation = queue.Track(jobId);

        return new BackgroundJob(jobId, jobName ?? "anonymous", work, cancellation.Token);
    }

    private async Task DelayThenEnqueueAsync(BackgroundJob job, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, job.CancellationToken);
            await queue.EnqueueAsync(job, job.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelled before it ever reached the queue; nothing ran, so the
            // processor will never get the chance to release the job.
            queue.Release(job.Id);
        }
    }
}
