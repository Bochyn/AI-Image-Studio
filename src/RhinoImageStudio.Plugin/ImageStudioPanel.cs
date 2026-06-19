using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Eto.Forms;
using Eto.Wpf.Forms;
using Eto.Wpf.Forms.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Rhino;
using Rhino.UI;

namespace RhinoImageStudio.Plugin;

/// <summary>
/// Image Studio dockable panel with WebView2 hosting React UI
/// </summary>
[Guid("B2C3D4E5-F678-9012-CDEF-234567890ABC")]
public class ImageStudioPanel : Eto.Forms.Panel, IPanel
{
    private WebView2? _webView;
    private RhinoBridge? _rhinoBridge;
    private bool _isInitialized;

    public static Guid PanelId => typeof(ImageStudioPanel).GUID;

    public ImageStudioPanel(uint documentSerialNumber)
    {
        InitializePanel();
    }

    public ImageStudioPanel()
    {
        InitializePanel();
    }

    private void InitializePanel()
    {
        // Create WPF WebView2 control
        _webView = new WebView2();

        // Create the Eto native control host for WPF
        var nativeHost = new Eto.Wpf.Forms.Controls.NativeControlHandler(_webView);
        var nativeControl = new Eto.Forms.Control(nativeHost);

        // Set up layout
        var layout = new DynamicLayout { Padding = 0, Spacing = new Eto.Drawing.Size(0, 0) };
        layout.Add(nativeControl, true, true);

        Content = layout;

        // Initialize WebView2 async
        InitializeWebView2Async();
    }

    private async void InitializeWebView2Async()
    {
        try
        {
            // Ensure backend is running
            var backend = RhinoImageStudioPlugIn.Instance.BackendManager;
            var started = await backend.StartAsync();

            if (!started)
            {
                RhinoApp.WriteLine("Failed to start backend - panel may not function correctly");
            }

            // Initialize WebView2
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RhinoImageStudio",
                "WebView2"
            );

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _webView!.EnsureCoreWebView2Async(env);

            // Configure WebView2
            _webView.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // Enable for debugging

            // Set up the Rhino bridge for JS interop
            _rhinoBridge = new RhinoBridge(backend.Port);
            _webView.CoreWebView2.AddHostObjectToScript("rhino", _rhinoBridge);

            // Handle messages from JavaScript
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // Navigate to the UI
            var uiUrl = $"{backend.BaseUrl}/index.html";
            _webView.Source = new Uri(uiUrl);

            _isInitialized = true;
            RhinoApp.WriteLine($"Image Studio panel initialized: {uiUrl}");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Failed to initialize WebView2: {ex.Message}");
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            RhinoApp.WriteLine($"Received message from UI: {message}");

            // Handle messages that need to run on UI thread
            // Messages are processed by the RhinoBridge host object
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error processing web message: {ex.Message}");
        }
    }

    #region IPanel Implementation

    public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
    {
        RhinoApp.WriteLine($"Image Studio panel shown (reason: {reason})");

        // Reinitialize if needed
        if (!_isInitialized)
        {
            InitializeWebView2Async();
        }
    }

    public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
    {
        RhinoApp.WriteLine($"Image Studio panel hidden (reason: {reason})");
    }

    public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
    {
        RhinoApp.WriteLine("Image Studio panel closing");
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _webView?.Dispose();
            _webView = null;
        }

        base.Dispose(disposing);
    }
}
