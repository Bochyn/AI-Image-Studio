using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Client service for fal.ai API interactions
/// </summary>
public interface IFalAiClient
{
    Task<string> UploadImageAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default);
    Task<FalQueueResponse> SubmitAsync(string modelId, object input, CancellationToken cancellationToken = default);
    Task<FalResultResponse> RunSyncAsync(string modelId, object input, CancellationToken cancellationToken = default);
    Task<FalStatusResponse> GetStatusAsync(string modelId, string requestId, string? statusUrl = null, CancellationToken cancellationToken = default);
    Task<FalResultResponse> GetResultAsync(string modelId, string requestId, string? responseUrl = null, CancellationToken cancellationToken = default);
    Task CancelAsync(string modelId, string requestId, CancellationToken cancellationToken = default);
}

public class FalAiClient : IFalAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretStorage _secretStorage;
    private readonly ILogger<FalAiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string FalQueueBaseUrl = "https://queue.fal.run";
    private const string FalRestApiUrl = "https://rest.alpha.fal.ai";

    public FalAiClient(
        HttpClient httpClient,
        ISecretStorage secretStorage,
        ILogger<FalAiClient> logger)
    {
        _httpClient = httpClient;
        _secretStorage = secretStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private async Task<string> GetApiKeyAsync()
    {
        var apiKey = await _secretStorage.GetSecretAsync("fal_api_key");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("fal.ai API key not configured. Please set it in Settings.");
        }
        return apiKey;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var apiKey = await GetApiKeyAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
        return request;
    }

    public async Task<string> UploadImageAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading image to fal.ai storage: {FileName} ({Size} bytes)", fileName, imageData.Length);

        // Step 1: Initiate upload to get presigned URL
        var initiateUrl = $"{FalRestApiUrl}/storage/upload/initiate?storage_type=fal-cdn-v3";
        var initiateRequest = await CreateRequestAsync(HttpMethod.Post, initiateUrl);

        var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        var initiatePayload = new { file_name = fileName, content_type = contentType };
        initiateRequest.Content = new StringContent(
            JsonSerializer.Serialize(initiatePayload, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var initiateResponse = await _httpClient.SendAsync(initiateRequest, cancellationToken);
        initiateResponse.EnsureSuccessStatusCode();

        var initiateJson = await initiateResponse.Content.ReadAsStringAsync(cancellationToken);
        var initiateResult = JsonSerializer.Deserialize<FalUploadInitiateResponse>(initiateJson, _jsonOptions);

        if (string.IsNullOrEmpty(initiateResult?.UploadUrl) || string.IsNullOrEmpty(initiateResult?.FileUrl))
        {
            throw new InvalidOperationException("Failed to initiate upload: missing URLs in response");
        }

        _logger.LogInformation("Upload initiated, uploading to presigned URL...");

        // Step 2: PUT the file to the presigned URL
        using var uploadContent = new ByteArrayContent(imageData);
        uploadContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var uploadRequest = new HttpRequestMessage(HttpMethod.Put, initiateResult.UploadUrl);
        uploadRequest.Content = uploadContent;

        var uploadResponse = await _httpClient.SendAsync(uploadRequest, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        _logger.LogInformation("Image uploaded successfully: {Url}", initiateResult.FileUrl);
        return initiateResult.FileUrl;
    }

    public async Task<FalQueueResponse> SubmitAsync(string modelId, object input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting job to fal.ai: {ModelId}", modelId);

        var url = $"{FalQueueBaseUrl}/{modelId}";
        var request = await CreateRequestAsync(HttpMethod.Post, url);

        var jsonContent = JsonSerializer.Serialize(input, _jsonOptions);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("Submit response: {Response}", responseJson.Length > 500 ? responseJson[..500] : responseJson);
        var queueResponse = JsonSerializer.Deserialize<FalQueueResponse>(responseJson, _jsonOptions);

        _logger.LogInformation("Job submitted: {RequestId}, StatusUrl: {StatusUrl}, ResponseUrl: {ResponseUrl}",
            queueResponse?.RequestId, queueResponse?.StatusUrl, queueResponse?.ResponseUrl);
        return queueResponse ?? throw new InvalidOperationException("Failed to parse queue response");
    }

    public async Task<FalResultResponse> RunSyncAsync(string modelId, object input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running synchronous job on fal.ai: {ModelId}", modelId);

        var url = $"https://fal.run/{modelId}";
        var request = await CreateRequestAsync(HttpMethod.Post, url);

        var jsonContent = JsonSerializer.Serialize(input, _jsonOptions);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Synchronous job completed for {ModelId}", modelId);

        return JsonSerializer.Deserialize<FalResultResponse>(responseJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse sync result");
    }

    public async Task<FalStatusResponse> GetStatusAsync(string modelId, string requestId, string? statusUrl = null, CancellationToken cancellationToken = default)
    {
        // Use status URL from queue response if available (partner models have different paths)
        var url = statusUrl ?? $"{FalQueueBaseUrl}/{modelId}/requests/{requestId}/status";
        var request = await CreateRequestAsync(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Partner models may require POST instead of GET
        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            _logger.LogDebug("GET not supported for status, trying POST: {Url}", url);
            var postRequest = await CreateRequestAsync(HttpMethod.Post, url);
            response = await _httpClient.SendAsync(postRequest, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<FalStatusResponse>(responseJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse status response");
    }

    public async Task<FalResultResponse> GetResultAsync(string modelId, string requestId, string? responseUrl = null, CancellationToken cancellationToken = default)
    {
        // Use response URL from queue response if available (partner models have different paths)
        var url = responseUrl ?? $"{FalQueueBaseUrl}/{modelId}/requests/{requestId}";
        var request = await CreateRequestAsync(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Partner models may require POST instead of GET
        if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            _logger.LogDebug("GET not supported for result, trying POST: {Url}", url);
            var postRequest = await CreateRequestAsync(HttpMethod.Post, url);
            response = await _httpClient.SendAsync(postRequest, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<FalResultResponse>(responseJson, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse result response");
    }

    public async Task CancelAsync(string modelId, string requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Canceling fal.ai job: {RequestId}", requestId);

        var url = $"{FalQueueBaseUrl}/{modelId}/requests/{requestId}/cancel";
        var request = await CreateRequestAsync(HttpMethod.Post, url);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Job canceled: {RequestId}", requestId);
    }
}

// Response DTOs for fal.ai API

public record FalUploadInitiateResponse(
    [property: JsonPropertyName("upload_url")] string UploadUrl,
    [property: JsonPropertyName("file_url")] string FileUrl
);

public record FalQueueResponse(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("response_url")] string? ResponseUrl = null,
    [property: JsonPropertyName("status_url")] string? StatusUrl = null
);

public record FalStatusResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("logs")] List<FalLogEntry>? Logs
);

public record FalLogEntry(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] string? Timestamp
);

public record FalResultResponse(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("images")] List<FalImage>? Images,
    [property: JsonPropertyName("image")] FalImage? Image,
    [property: JsonPropertyName("seed")] long? Seed,
    [property: JsonPropertyName("prompt")] string? Prompt
);

public record FalImage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("content_type")] string? ContentType
);
