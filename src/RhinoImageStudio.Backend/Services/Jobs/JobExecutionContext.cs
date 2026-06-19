using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class JobExecutionContext
{
    public required Job Job { get; init; }
    public required AppDbContext Db { get; init; }
    public required IFalAiClient FalClient { get; init; }
    public required IGeminiClient GeminiClient { get; init; }
    public required ISecretStorage SecretStorage { get; init; }
    public required IStorageService Storage { get; init; }
    public required JobProgressBroadcaster Progress { get; init; }
    public required JobResultPersister Results { get; init; }
    public required FalJobPoller FalPoller { get; init; }
}
