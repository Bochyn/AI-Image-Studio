using Rhino;

namespace RhinoImageStudio.Plugin.RhinoCommon;

/// <summary>
/// Marshals work onto the Rhino UI thread and returns the result asynchronously.
/// </summary>
public static class RhinoUiThread
{
    public static Task<T?> RunAsync<T>(Func<T?> action, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);

        RhinoApp.InvokeOnUiThread(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                completion.TrySetResult(action());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    public static Task RunAsync(Action action, CancellationToken cancellationToken = default) =>
        RunAsync<object?>(() =>
        {
            action();
            return null;
        }, cancellationToken);
}
