using System.Net;
using System.Net.Sockets;

namespace RhinoImageStudio.Plugin.RhinoCommon;

public static class BackendPortUtilities
{
    public static int FindAvailablePort(int preferredPort)
    {
        if (IsPortAvailable(preferredPort))
            return preferredPort;

        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    public static bool IsPortAvailable(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        try
        {
            listener.Start();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            listener.Stop();
        }
    }
}
