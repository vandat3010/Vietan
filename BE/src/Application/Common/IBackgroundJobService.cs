namespace Backend.Application.Common;

/// <summary>
/// Hands work off to run outside the current request, so an HTTP response is
/// never held hostage by e-mail delivery, report generation or a slow webhook.
/// <para>
/// Work is expressed as <c>Func&lt;IServiceProvider, CancellationToken, Task&gt;</c>
/// rather than a closure over the caller's dependencies: the job runs after the
/// request scope is disposed, so it must resolve its own scoped services
/// (DbContext, repositories) from the provider it is handed. Capturing them
/// instead is the classic source of "Cannot access a disposed context".
/// </para>
/// <para>
/// Implementations may be in-process (the default) or a durable scheduler such
/// as Hangfire or Quartz; the returned job id is the only handle callers get.
/// </para>
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Queues <paramref name="work"/> for execution as soon as a worker is free and
    /// returns its job id.
    /// <para>
    /// Deliberately named Enqueue rather than Execute: an "ExecuteAsync" that the
    /// caller awaits would be indistinguishable from just calling the work
    /// directly, and would defeat the purpose of getting off the request thread.
    /// The returned <see cref="Task"/> completes once the job is *accepted*, not
    /// once it has run.
    /// </para>
    /// </summary>
    Task<string> EnqueueAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        string? jobName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Same as <see cref="EnqueueAsync"/> but held back for <paramref name="delay"/> first.</summary>
    Task<string> ScheduleAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        TimeSpan delay,
        string? jobName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cancellation of a queued, scheduled or already running job.
    /// Returns false when the id is unknown - typically because the job has
    /// already finished. A running job only stops if its body observes the
    /// cancellation token it is given.
    /// </summary>
    Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default);
}
