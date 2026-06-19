using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Rhino;
using RhinoImageStudio.Plugin.RhinoCommon;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Plugin.Mac;

public sealed class MacBackendManager : IDisposable
{
    private Process? _backendProcess;
    private MacRhinoBridgeClient? _bridgeClient;
    private bool _isDisposed;

    public int Port { get; private set; }
    public string BaseUrl => $"http://localhost:{Port}";
    public bool IsRunning => _backendProcess is { HasExited: false };

    public async Task<bool> StartAsync()
    {
        if (IsRunning)
        {
            RhinoApp.WriteLine($"Rhino Image Studio backend already running on port {Port}.");
            StartBridge();
            return true;
        }

        if (await TryConnectToExistingBackendAsync(Defaults.DefaultPort).ConfigureAwait(false))
        {
            Port = Defaults.DefaultPort;
            RhinoApp.WriteLine($"Connected to existing Rhino Image Studio backend on port {Port}.");
            StartBridge();
            return true;
        }

        var backendExecutablePath = GetBackendExecutablePath();
        var backendDllPath = GetBackendDllPath();
        if (!File.Exists(backendExecutablePath) && !File.Exists(backendDllPath))
        {
            RhinoApp.WriteLine($"Rhino Image Studio backend not found: {backendDllPath}");
            return false;
        }

        var startInfo = CreateStartInfo(backendExecutablePath, backendDllPath);
        if (startInfo == null)
        {
            RhinoApp.WriteLine("No .NET runtime found for Rhino Image Studio backend.");
            return false;
        }

        Port = BackendPortUtilities.FindAvailablePort(Defaults.DefaultPort);
        startInfo.Arguments = string.IsNullOrEmpty(startInfo.Arguments)
            ? $"--port={Port}"
            : $"{startInfo.Arguments} --port={Port}";

        _backendProcess = new Process { StartInfo = startInfo };
        _backendProcess.OutputDataReceived += (_, args) => WriteBackendLine(args.Data, "Backend");
        _backendProcess.ErrorDataReceived += (_, args) => WriteBackendLine(args.Data, "Backend Error");

        RhinoApp.WriteLine($"Starting Rhino Image Studio backend on {BaseUrl}...");
        _backendProcess.Start();
        _backendProcess.BeginOutputReadLine();
        _backendProcess.BeginErrorReadLine();

        var ready = await WaitForBackendReadyAsync().ConfigureAwait(false);
        if (ready)
            StartBridge();

        return ready;
    }

    public void Stop()
    {
        _bridgeClient?.Dispose();
        _bridgeClient = null;

        if (_backendProcess == null || _backendProcess.HasExited)
            return;

        try
        {
            _backendProcess.Kill(entireProcessTree: true);
            _backendProcess.WaitForExit(3000);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Failed to stop Rhino Image Studio backend: {ex.Message}");
        }
        finally
        {
            _backendProcess.Dispose();
            _backendProcess = null;
        }
    }

    private static void WriteBackendLine(string? line, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(line))
            RhinoApp.WriteLine($"[{prefix}] {line}");
    }

    private void StartBridge()
    {
        _bridgeClient?.Dispose();
        _bridgeClient = new MacRhinoBridgeClient(BaseUrl);
        _bridgeClient.Start();
    }

    private static string GetBackendDllPath()
    {
        var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(pluginDirectory, "Backend", "RhinoImageStudio.Backend.dll");
    }

    private static string GetBackendExecutablePath()
    {
        var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(pluginDirectory, "Backend", "RhinoImageStudio.Backend");
    }

    private static ProcessStartInfo? CreateStartInfo(string backendExecutablePath, string backendDllPath)
    {
        if (File.Exists(backendExecutablePath))
        {
            return new ProcessStartInfo
            {
                FileName = backendExecutablePath,
                WorkingDirectory = Path.GetDirectoryName(backendExecutablePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        var dotnetPath = GetDotnetPath();
        if (string.IsNullOrWhiteSpace(dotnetPath))
            return null;

        return new ProcessStartInfo
        {
            FileName = dotnetPath,
            Arguments = $"\"{backendDllPath}\"",
            WorkingDirectory = Path.GetDirectoryName(backendDllPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static string? GetDotnetPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("RHINO_IMAGE_STUDIO_DOTNET");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        var architecture = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "x86_64" : "arm64";
        var rhinoDotnetPath = Path.Combine(
            "/Applications/Rhino 8.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources/dotnet",
            architecture,
            "dotnet"
        );
        if (File.Exists(rhinoDotnetPath))
            return rhinoDotnetPath;

        return "dotnet";
    }

    private static async Task<bool> TryConnectToExistingBackendAsync(int port) =>
        await BackendHealthChecker.IsHealthyAsync(port).ConfigureAwait(false);

    private async Task<bool> WaitForBackendReadyAsync(int timeoutSeconds = 30) =>
        await BackendHealthChecker.WaitUntilHealthyAsync(
            BaseUrl,
            () => _backendProcess?.HasExited == true,
            timeoutSeconds).ConfigureAwait(false);

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
    }
}
