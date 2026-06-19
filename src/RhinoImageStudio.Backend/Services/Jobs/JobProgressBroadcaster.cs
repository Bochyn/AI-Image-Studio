using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class JobProgressBroadcaster
{
    private readonly IEventBroadcaster _eventBroadcaster;

    public JobProgressBroadcaster(IEventBroadcaster eventBroadcaster)
    {
        _eventBroadcaster = eventBroadcaster;
    }

    public void Report(Job job, int progress, string? message, Guid? resultId = null)
    {
        job.Progress = progress;
        job.ProgressMessage = message;
        if (resultId.HasValue)
            job.ResultId = resultId;

        var jobDto = new JobDto(
            Id: job.Id,
            ProjectId: job.ProjectId,
            Type: job.Type,
            Status: job.Status,
            Progress: progress,
            ProgressMessage: message,
            ErrorMessage: job.ErrorMessage,
            ResultId: resultId,
            CreatedAt: job.CreatedAt,
            StartedAt: job.StartedAt,
            CompletedAt: job.CompletedAt);

        _eventBroadcaster.BroadcastToProject(job.ProjectId, jobDto);
    }
}
