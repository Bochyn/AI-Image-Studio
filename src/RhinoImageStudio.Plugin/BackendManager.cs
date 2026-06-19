using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Rhino;
using RhinoImageStudio.Plugin.RhinoCommon;
using SharedDefaults = RhinoImageStudio.Shared.Constants.Defaults;

namespace RhinoImageStudio.Plugin;

/// <summary>
/// Manages the backend sidecar process lifecycle
/// </summary>
public class BackendManager : IDisposable
{
    private Process? _backendProcess;
    private int _port;
    private bool _isDisposed;
    private readonly string _backendPath;

    public int Port => _port;
    public string BaseUrl => $"http://localhost:{_port}";
    public bool IsRunning => _backendProcess is { HasExited: false };

    public BackendManager()
    {
        // Find backend executable relative to plugin location
        var pluginDir = Path.GetDirectoryName(typeof(BackendManager).Assembly.Location)!;
        _backendPath = Path.Combine(pluginDir, "Backend", "RhinoImageStudio.Backend.exe");

        if (!File.Exists(_backendPath))
        {
            // Try development path
            var devPath = Path.Combine(pluginDir, "..", "..", "..", "..", "RhinoImageStudio.Backend", "bin", "Debug", "net8.0-windows", "RhinoImageStudio.Backend.exe");
            if (File.Exists(devPath))
            {
                _backendPath = devPath;
            }
        }
    }

    /// <summary>
    /// Starts the backend process if not already running
    /// </summary>
    public async Task<bool> StartAsync()
    {
        if (IsRunning)
        {
            RhinoApp.WriteLine($"Backend already running on port {_port}");
            return true;
        }

        // First, check if backend is already running externally on the default port
        if (await TryConnectToExistingBackendAsync(SharedDefaults.DefaultPort))
        {
            _port = SharedDefaults.DefaultPort;
            RhinoApp.WriteLine($"Connected to existing backend on port {_port}");
            return true;
        }

        if (!File.Exists(_backendPath))
        {
            RhinoApp.WriteLine($"Backend executable not found: {_backendPath}");
            RhinoApp.WriteLine($"Please start the backend manually: dotnet run in RhinoImageStudio.Backend folder");
            RhinoApp.WriteLine($"Backend should be running on http://localhost:{SharedDefaults.DefaultPort}");
            return false;
        }

        try
        {
            // Find an available port
            _port = FindAvailablePort();

            RhinoApp.WriteLine($"Starting backend on port {_port}...");

            var startInfo = new ProcessStartInfo
            {
                FileName = _backendPath,
                Arguments = $"--port={_port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_backendPath)
            };

            _backendProcess = new Process { StartInfo = startInfo };

            _backendProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    RhinoApp.WriteLine($"[Backend] {e.Data}");
                }
            };

            _backendProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    RhinoApp.WriteLine($"[Backend Error] {e.Data}");
                }
            };

            _backendProcess.Start();
            _backendProcess.BeginOutputReadLine();
            _backendProcess.BeginErrorReadLine();

            // Wait for backend to be ready
            var ready = await WaitForBackendReadyAsync();
            if (ready)
            {
                RhinoApp.WriteLine($"Backend started successfully on {BaseUrl}");
            }
            else
            {
                RhinoApp.WriteLine("Backend failed to start within timeout");
            }

            return ready;
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Failed to start backend: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to connect to an existing backend on a given port
    /// Uses synchronous call to avoid async/await deadlock in Rhino's UI context
    /// </summary>
    private async Task<bool> TryConnectToExistingBackendAsync(int port)
    {
        RhinoApp.WriteLine($"Checking for existing backend on port {port}...");
        return await Task.Run(() => BackendHealthChecker.IsHealthySync(port)).ConfigureAwait(false);
    }

    private static int FindAvailablePort() =>
        BackendPortUtilities.FindAvailablePort(SharedDefaults.DefaultPort);

    /// <summary>
    /// Stops the backend process
    /// </summary>
    public void Stop()
    {
        if (_backendProcess == null || _backendProcess.HasExited)
            return;

        try
        {
            RhinoApp.WriteLine("Stopping backend...");

            // Try graceful shutdown first
            _backendProcess.CloseMainWindow();

            if (!_backendProcess.WaitForExit(3000))
            {
                // Force kill if graceful shutdown fails
                _backendProcess.Kill();
            }

            RhinoApp.WriteLine("Backend stopped.");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error stopping backend: {ex.Message}");
        }
        finally
        {
            _backendProcess?.Dispose();
            _backendProcess = null;
        }
    }

    private async Task<bool> WaitForBackendReadyAsync(int timeoutSeconds = 30) =>
        await BackendHealthChecker.WaitUntilHealthyAsync(
            BaseUrl,
            () => _backendProcess?.HasExited == true,
            timeoutSeconds).ConfigureAwait(false);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();
        GC.SuppressFinalize(this);
    }
}
