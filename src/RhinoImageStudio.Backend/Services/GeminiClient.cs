using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Client service for Google Gemini API interactions (image generation with Nano Banana / gemini-2.5-flash-image)
/// </summary>

/// <summary>
/// Represents a mask image with its editing instruction for inpainting
/// </summary>
public record MaskImageData(byte[] ImageData, string Instruction);

public interface IGeminiClient
{
    Task<GeminiImageResult> GenerateImageAsync(string prompt, byte[]? sourceImage = null, byte[]? overlayImage = null, List<byte[]>? referenceImages = null, GeminiImageConfig? config = null, List<MaskImageData>? maskImages = null, CancellationToken cancellationToken = default);
    Task<GeminiImageResult> EditImageAsync(string prompt, byte[] sourceImage, List<byte[]>? referenceImages = null, GeminiImageConfig? config = null, CancellationToken cancellationToken = default);
}

public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretStorage _secretStorage;
    private readonly ILogger<GeminiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultModel = "gemini-3.1-flash-image-preview";

    public GeminiClient(
        HttpClient httpClient,
        ISecretStorage secretStorage,
        ILogger<GeminiClient> logger)
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
        var apiKey = await _secretStorage.GetSecretAsync("gemini_api_key");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini API key not configured. Please set it in Settings.");
        }
        return apiKey;
    }

    public async Task<GeminiImageResult> GenerateImageAsync(
        string prompt,
        byte[]? sourceImage = null,
        byte[]? overlayImage = null,
        List<byte[]>? referenceImages = null,
        GeminiImageConfig? config = null,
        List<MaskImageData>? maskImages = null,
        CancellationToken cancellationToken = default)
    {
        config ??= new GeminiImageConfig();

        _logger.LogInformation("Generating image with Gemini. Prompt: {Prompt}", prompt.Substring(0, Math.Min(50, prompt.Length)));

        var parts = new List<object> { new { text = prompt } };

        // Add source image if provided (image-to-image editing)
        if (sourceImage != null && sourceImage.Length > 0)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = "image/png",
                    data = Convert.ToBase64String(sourceImage)
                }
            });
        }

        // Add overlay image if provided (IMAGE 2 — colored mask guide)
        if (overlayImage != null && overlayImage.Length > 0)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = "image/png",
                    data = Convert.ToBase64String(overlayImage)
                }
            });
        }

        // Add reference images if provided
        if (referenceImages != null)
        {
            foreach (var refImage in referenceImages)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/png",
                        data = Convert.ToBase64String(refImage)
                    }
                });
            }
        }

        // Add mask images if provided (for inpainting)
        if (maskImages != null)
        {
            foreach (var mask in maskImages)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/png",
                        data = Convert.ToBase64String(mask.ImageData)
                    }
                });
            }
        }

        // Gemini image generation: responseModalities tells it to return images
        // imageConfig contains aspectRatio; imageSize only supported by Gemini 3 Pro+
        var model = config.Model ?? DefaultModel;
        var supportsImageSize = ModelCapabilityLimits.SupportsImageSize(model);

        object imageConfig = supportsImageSize && config.ImageSize != null
            ? new { aspectRatio = config.AspectRatio ?? "1:1", imageSize = config.ImageSize }
            : new { aspectRatio = config.AspectRatio ?? "1:1" };

        var requestBody = new
        {
            contents = new[]
            {
                new { parts }
            },
            generationConfig = new
            {
                responseModalities = new[] { "TEXT", "IMAGE" },
                imageConfig
            }
        };

        return await SendRequestAsync(requestBody, model, cancellationToken);
    }

    public async Task<GeminiImageResult> EditImageAsync(
        string prompt,
        byte[] sourceImage,
        List<byte[]>? referenceImages = null,
        GeminiImageConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceImage == null || sourceImage.Length == 0)
        {
            throw new ArgumentException("Source image is required for image editing", nameof(sourceImage));
        }

        return await GenerateImageAsync(prompt, sourceImage, overlayImage: null, referenceImages, config, maskImages: null, cancellationToken);
    }

    private async Task<GeminiImageResult> SendRequestAsync(
        object requestBody,
        string model,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetApiKeyAsync();
        var url = $"{GeminiApiBaseUrl}/{model}:generateContent?key={apiKey}";

        var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending request to Gemini API: {Model}", model);

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
            throw new GeminiApiException($"Gemini API request failed: {response.StatusCode}", responseJson);
        }

        return ParseResponse(responseJson);
    }

    private GeminiImageResult ParseResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Check for error
            if (root.TryGetProperty("error", out var errorElement))
            {
                var errorMessage = errorElement.TryGetProperty("message", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown Gemini API error";
                throw new GeminiApiException(errorMessage ?? "Unknown error", responseJson);
            }

            // Extract candidates
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                throw new GeminiApiException("No candidates in response", responseJson);
            }

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var contentElement) ||
                !contentElement.TryGetProperty("parts", out var parts))
            {
                throw new GeminiApiException("Invalid response structure", responseJson);
            }

            string? textResponse = null;
            byte[]? imageData = null;
            string? mimeType = null;

            foreach (var part in parts.EnumerateArray())
            {
                // Check for text
                if (part.TryGetProperty("text", out var textElement))
                {
                    textResponse = textElement.GetString();
                }

                // Check for image (can be "inlineData" or "inline_data")
                JsonElement? inlineDataElement = null;
                if (part.TryGetProperty("inlineData", out var inlineData1))
                {
                    inlineDataElement = inlineData1;
                }
                else if (part.TryGetProperty("inline_data", out var inlineData2))
                {
                    inlineDataElement = inlineData2;
                }

                if (inlineDataElement.HasValue)
                {
                    var inlineData = inlineDataElement.Value;
                    if (inlineData.TryGetProperty("mimeType", out var mimeElement) ||
                        inlineData.TryGetProperty("mime_type", out mimeElement))
                    {
                        mimeType = mimeElement.GetString();
                    }

                    if (inlineData.TryGetProperty("data", out var dataElement))
                    {
                        var base64Data = dataElement.GetString();
                        if (!string.IsNullOrEmpty(base64Data))
                        {
                            imageData = Convert.FromBase64String(base64Data);
                        }
                    }
                }
            }

            if (imageData == null)
            {
                throw new GeminiApiException("No image data in response", responseJson);
            }

            _logger.LogInformation("Gemini image generated successfully. Size: {Size} bytes", imageData.Length);

            return new GeminiImageResult(
                ImageData: imageData,
                MimeType: mimeType ?? "image/jpeg",
                TextResponse: textResponse
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response");
            throw new GeminiApiException("Failed to parse Gemini API response", responseJson);
        }
    }
}

// Configuration for Gemini image generation
public record GeminiImageConfig(
    string? Model = null,
    string OutputFormat = "jpeg",
    string? AspectRatio = null,
    string? ImageSize = null  // e.g., "1K", "2K", "4K"
);

// Result from Gemini image generation
public record GeminiImageResult(
    byte[] ImageData,
    string MimeType,
    string? TextResponse = null
);

// Exception for Gemini API errors
public class GeminiApiException : Exception
{
    public string RawResponse { get; }

    public GeminiApiException(string message, string rawResponse) : base(message)
    {
        RawResponse = rawResponse;
    }
}
