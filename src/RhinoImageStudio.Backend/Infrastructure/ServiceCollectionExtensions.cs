using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Options;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Services.Jobs;

namespace RhinoImageStudio.Backend.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRhinoImageStudioServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IEventBroadcaster, EventBroadcaster>();
        services.AddSingleton<IBridgeTokenService, BridgeTokenService>();
        services.AddSingleton<IRhinoBridgeService, RhinoBridgeService>();
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<SecretStorageOptions>(configuration.GetSection(SecretStorageOptions.SectionName));

        var secretStorageOptions = configuration
            .GetSection(SecretStorageOptions.SectionName)
            .Get<SecretStorageOptions>() ?? new SecretStorageOptions();
        var dataProtectionKeysPath = Path.Combine(secretStorageOptions.StoragePath, "keys");
        Directory.CreateDirectory(dataProtectionKeysPath);

        services.AddDataProtection()
            .SetApplicationName("RhinoImageStudio")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        services.AddSingleton<ISecretStorage, DataProtectionSecretStorage>();
        services.AddSingleton<IStorageService, StorageService>();

        services.AddHttpClient<IFalAiClient, FalAiClient>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                options.Retry.MaxRetryAttempts = 2;
            });
        services.AddHttpClient<IGeminiClient, GeminiClient>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                options.Retry.MaxRetryAttempts = 2;
            });
        services.AddHttpClient("ImageDownloader")
            .AddStandardResilienceHandler();

        services.AddSingleton<JobProgressBroadcaster>();
        services.AddSingleton<JobResultPersister>();
        services.AddSingleton<FalJobPoller>();
        services.AddSingleton<IJobHandler, GenerateJobHandler>();
        services.AddSingleton<IJobHandler, RefineJobHandler>();
        services.AddSingleton<IJobHandler, MultiAngleJobHandler>();
        services.AddSingleton<IJobHandler, UpscaleJobHandler>();
        services.AddHostedService<JobProcessor>();

        return services;
    }
}
