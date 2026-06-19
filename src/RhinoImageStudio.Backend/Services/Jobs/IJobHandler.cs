using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Backend.Services.Jobs;

public interface IJobHandler
{
    JobType JobType { get; }

    Task<Guid> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
}
