using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Drains <see cref="BackgroundJobQueue"/> for the lifetime of the host. Jobs run
/// one at a time: sequential execution keeps the failure modes obvious and stops
/// a burst of jobs from starving the thread pool that is serving HTTP requests.
/// <para>
/// Every job gets its OWN DI scope, because the request scope that enqueued it is
/// already disposed by the time it runs - resolving a DbContext from the root
/// provider instead would share one instance across unrelated jobs.
/// </para>
/// </summary>
public sealed class BackgroundJobProcessor(
    BackgroundJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in queue.ReadAllAsync(stoppingToken))
            {
                await RunAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <remarks>
    /// The catch-all is deliberate: an exception escaping
    /// <see cref="ExecuteAsync"/> tears down the whole host, so one badly behaved
    /// job would take every future job - and the application - with it.
    /// </remarks>
    private async Task RunAsync(BackgroundJob job, CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.CancellationToken);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await job.Work(scope.ServiceProvider, cancellation.Token);

            logger.LogInformation("Background job {JobName} ({JobId}) completed.", job.Name, job.Id);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Background job {JobName} ({JobId}) was cancelled.", job.Name, job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background job {JobName} ({JobId}) failed.", job.Name, job.Id);
        }
        finally
        {
            queue.Release(job.Id);
        }
    }
}
