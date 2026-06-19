using System.Net.Http;

namespace RhinoImageStudio.Plugin.RhinoCommon;

public static class BackendHealthChecker
{
    public static async Task<bool> IsHealthyAsync(int port, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var response = await httpClient
                .GetAsync($"http://localhost:{port}/api/health", cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsHealthySync(int port, TimeSpan? timeout = null)
    {
        using var httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(5) };
        try
        {
            var response = httpClient.GetAsync($"http://localhost:{port}/api/health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> WaitUntilHealthyAsync(
        string baseUrl,
        Func<bool>? processExited,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var healthUrl = $"{baseUrl.TrimEnd('/')}/api/health";

        for (var i = 0; i < timeoutSeconds * 2; i++)
        {
            if (processExited?.Invoke() == true)
                return false;

            try
            {
                using var response = await httpClient.GetAsync(healthUrl, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // Backend is still starting.
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
