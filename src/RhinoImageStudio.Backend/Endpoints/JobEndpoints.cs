using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Infrastructure;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Validation;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Endpoints;

public static class JobEndpoints
{
    public static RouteGroupBuilder MapJobEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/jobs", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var jobs = await db.Jobs
                .AsNoTracking()
                .Where(j => j.ProjectId == projectId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(jobs.Select(j => j.ToDto()).ToList());
        });

        api.MapPost("/jobs/{id:guid}/cancel", async (
            Guid id,
            AppDbContext db,
            IFalAiClient falClient,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var job = await db.Jobs.FindAsync(new object[] { id }, ct);
            if (job is null) return Results.NotFound();

            var logger = loggerFactory.CreateLogger("JobEndpoints");

            if (job.Status is JobStatus.Queued or JobStatus.Running)
            {
                if (!string.IsNullOrEmpty(job.FalRequestId))
                {
                    var modelId = FalModelResolver.ResolveModelId(job);
                    try
                    {
                        await falClient.CancelAsync(modelId, job.FalRequestId, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to cancel fal.ai job {FalRequestId} for job {JobId}", job.FalRequestId, job.Id);
                    }
                }

                job.Status = JobStatus.Canceled;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(job.ToDto());
        }).RequireLocalApiToken();

        api.MapPost("/generate", async (GenerateRequest request, AppDbContext db, IJobQueue jobQueue, CancellationToken ct) =>
        {
            var validationError = GenerateRequestValidator.Validate(request)
                ?? await ProjectValidator.EnsureExistsAsync(db, request.ProjectId, ct);
            if (validationError != null)
                return Results.BadRequest(validationError);

            var job = await EnqueueJobAsync(db, jobQueue, request.ProjectId, JobType.Generate, request, request.Model, ct);
            return Results.Accepted($"/api/jobs/{job.Id}", job.ToDto());
        }).RequireLocalApiToken();

        api.MapPost("/refine", async (RefineRequest request, AppDbContext db, IJobQueue jobQueue, CancellationToken ct) =>
        {
            var validationError = JobRequestValidators.Validate(request)
                ?? await ProjectValidator.EnsureExistsAsync(db, request.ProjectId, ct);
            if (validationError != null)
                return Results.BadRequest(validationError);

            var job = await EnqueueJobAsync(db, jobQueue, request.ProjectId, JobType.Refine, request, null, ct);
            return Results.Accepted($"/api/jobs/{job.Id}", job.ToDto());
        }).RequireLocalApiToken();

        api.MapPost("/multi-angle", async (MultiAngleRequest request, AppDbContext db, IJobQueue jobQueue, CancellationToken ct) =>
        {
            var validationError = JobRequestValidators.Validate(request)
                ?? await ProjectValidator.EnsureExistsAsync(db, request.ProjectId, ct);
            if (validationError != null)
                return Results.BadRequest(validationError);

            var job = await EnqueueJobAsync(
                db, jobQueue, request.ProjectId, JobType.MultiAngle, request, FalModels.QwenMultipleAngles, ct);
            return Results.Accepted($"/api/jobs/{job.Id}", job.ToDto());
        }).RequireLocalApiToken();

        api.MapPost("/upscale", async (UpscaleRequest request, AppDbContext db, IJobQueue jobQueue, CancellationToken ct) =>
        {
            var validationError = JobRequestValidators.Validate(request)
                ?? await ProjectValidator.EnsureExistsAsync(db, request.ProjectId, ct);
            if (validationError != null)
                return Results.BadRequest(validationError);

            var job = await EnqueueJobAsync(
                db, jobQueue, request.ProjectId, JobType.Upscale, request, FalModels.TopazUpscale, ct);
            return Results.Accepted($"/api/jobs/{job.Id}", job.ToDto());
        }).RequireLocalApiToken();

        return api;
    }

    private static async Task<Job> EnqueueJobAsync<T>(
        AppDbContext db,
        IJobQueue jobQueue,
        Guid projectId,
        JobType type,
        T request,
        string? providerModelId,
        CancellationToken ct)
    {
        var job = new Job
        {
            ProjectId = projectId,
            Type = type,
            RequestJson = JobRequestJson.Serialize(request),
            ProviderModelId = providerModelId
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        await jobQueue.EnqueueAsync(job, ct);
        return job;
    }
}
