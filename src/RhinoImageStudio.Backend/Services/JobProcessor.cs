using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Services.Jobs;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Background service that dispatches queued jobs to typed handlers.
/// </summary>
public sealed class JobProcessor : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyDictionary<JobType, IJobHandler> _handlers;
    private readonly JobProgressBroadcaster _progress;
    private readonly JobResultPersister _results;
    private readonly FalJobPoller _falPoller;
    private readonly ILogger<JobProcessor> _logger;

    public JobProcessor(
        IJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IJobHandler> handlers,
        JobProgressBroadcaster progress,
        JobResultPersister results,
        FalJobPoller falPoller,
        ILogger<JobProcessor> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _handlers = handlers.ToDictionary(handler => handler.JobType);
        _progress = progress;
        _results = results;
        _falPoller = falPoller;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job processor loop");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Job Processor stopped");
    }

    private async Task ProcessJobAsync(Job queuedJob, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await dbContext.Jobs.FindAsync(new object[] { queuedJob.Id }, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} no longer exists; skipping", queuedJob.Id);
            return;
        }

        if (job.Status == JobStatus.Canceled)
        {
            _logger.LogInformation("Skipping canceled job {JobId}", job.Id);
            return;
        }

        _logger.LogInformation("Processing job {JobId} of type {JobType}", job.Id, job.Type);

        if (!_handlers.TryGetValue(job.Type, out var handler))
            throw new NotSupportedException($"Job type {job.Type} not supported");

        try
        {
            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            dbContext.Jobs.Update(job);
            await dbContext.SaveChangesAsync(cancellationToken);

            _progress.Report(job, 0, "Starting...");

            var executionContext = new JobExecutionContext
            {
                Job = job,
                Db = dbContext,
                FalClient = scope.ServiceProvider.GetRequiredService<IFalAiClient>(),
                GeminiClient = scope.ServiceProvider.GetRequiredService<IGeminiClient>(),
                SecretStorage = scope.ServiceProvider.GetRequiredService<ISecretStorage>(),
                Storage = scope.ServiceProvider.GetRequiredService<IStorageService>(),
                Progress = _progress,
                Results = _results,
                FalPoller = _falPoller
            };

            var resultId = await handler.ExecuteAsync(executionContext, cancellationToken);

            job.Status = JobStatus.Succeeded;
            job.ResultId = resultId;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
            job.ProgressMessage = "Completed";
            dbContext.Jobs.Update(job);
            await dbContext.SaveChangesAsync(cancellationToken);

            _progress.Report(job, 100, "Completed", resultId);
            _logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            dbContext.Jobs.Update(job);
            await dbContext.SaveChangesAsync(cancellationToken);

            _progress.Report(job, job.Progress, $"Failed: {ex.Message}");
        }
    }
}
