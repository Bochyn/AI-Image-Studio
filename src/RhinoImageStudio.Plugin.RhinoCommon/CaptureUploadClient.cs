using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Plugin.RhinoCommon;

public static class CaptureUploadClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> UploadAsync(
        HttpClient httpClient,
        Guid projectId,
        byte[] imageBytes,
        int width,
        int height,
        string displayMode,
        string? viewName,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(imageBytes);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        form.Add(image, "image", "rhino-capture.png");
        form.Add(new StringContent(projectId.ToString()), "projectId");
        form.Add(new StringContent(width.ToString()), "width");
        form.Add(new StringContent(height.ToString()), "height");
        form.Add(new StringContent(displayMode), "displayMode");
        form.Add(new StringContent(viewName ?? "Viewport"), "viewName");

        using var response = await httpClient.PostAsync("api/captures", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CaptureUploadResponse>(json, JsonOptions);
        if (result == null || string.IsNullOrWhiteSpace(result.Id))
            throw new InvalidOperationException("Backend did not return a capture id.");

        return result.Id;
    }
}
