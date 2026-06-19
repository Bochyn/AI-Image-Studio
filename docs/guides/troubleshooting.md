# Troubleshooting

Solutions to the most common problems you may run into with Rhino Image Studio.

## Startup Problems

### Backend won't start (Port in use)
**Symptom**: Error `System.IO.IOException: Failed to bind to address http://127.0.0.1:17532`.
**Cause**: Another backend instance is already running, or the port is taken.
**Fix**:
1. Check whether another terminal window has `dotnet run` running.
2. Kill `RhinoImageStudio.Backend.exe` from Task Manager.
3. On macOS, check and stop the sidecar process:
   ```bash
   pgrep -fl 'RhinoImageStudio.Backend'
   pkill -f 'RhinoImageStudio.Backend --port=17532'
   ```

### Plugin doesn't load in Rhino
**Symptom**: Install error or no `RhinoImageStudio` command.
**Cause**: Wrong Rhino version or missing .NET Framework 4.8.
**Fix**:
- Make sure you're on **Rhino 8** for Windows.
- Check the file properties of the `.rhp` (right click → Properties) and confirm Windows hasn't blocked it ("Unblock").
- On macOS, reinstall with `scripts/install-mac-plugin.sh`, restart Rhino, then run:
  ```bash
  /Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioMacStatus
  cat "$HOME/Library/Application Support/RhinoImageStudio/mac-plugin-status.json"
  ```

### macOS bridge is disconnected
**Symptom**: `/api/rhino/status` returns `connected: false`, or viewport capture does not run from the browser UI.
**Cause**: Rhino is not running, the plug-in has not started the backend, or the long-poll bridge is not connected.
**Fix**:
1. Start Rhino 8.
2. Run `ImageStudioStartBackend` in Rhino.
3. Verify:
   ```bash
   curl -fsS http://localhost:17532/api/health
   curl -fsS http://localhost:17532/api/rhino/status
   ```
4. If status is still disconnected, restart Rhino and run `ImageStudioStartBackend` again.

### Blank white screen in the panel
**Symptom**: The panel opens but is empty.
**Cause**: Missing WebView2 Runtime or the backend isn't running.
**Fix**:
1. Make sure the backend is running (open `http://localhost:17532` in a browser — you should see the UI page).
2. Install the [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).

## Generation Problems

### "Authentication failed" error
**Cause**: Invalid or missing API key.
**Fix**: Check the Settings tab and make sure the key has no leading/trailing whitespace. Generate a fresh key on fal.ai.

### Generation runs forever
**Cause**: SSE connection problem or AI server overload.
**Fix**:
- Watch the progress bar — it should grow smoothly from 20 % to 85 % during generation.
- If the bar is frozen, check the backend console logs.
- Try restarting the backend.

### Progress bar doesn't update
**Cause**: Lost SSE (Server-Sent Events) connection.
**Fix**:
- The app automatically tries to resume the connection (up to 10 retries).
- If the issue persists, refresh the page (F5).
- In browser DevTools (F12 → Network), check whether the `/api/projects/{id}/events` connection is active.

### Result is very low quality / blurry
**Cause**: Capture resolution is too low, or the wrong AI model.
**Fix**:
- Increase the resolution in the Capture settings.
- Use the **Upscale** feature on the finished image.
