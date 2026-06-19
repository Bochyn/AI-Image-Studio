import type { RhinoBridge, ViewportInfo } from './types';

export type RhinoRuntime = 'webview2' | 'http' | 'none';

export interface RhinoBridgeStatus {
  runtime: RhinoRuntime;
  bridge: RhinoBridge | null;
  isAvailable: boolean;
  error?: string;
}

interface DisplayModeDto {
  name: string;
  id: string;
}

interface ViewportDto {
  name: string;
  isActive: boolean;
  width?: number;
  height?: number;
  displayMode?: string;
}

const DEFAULT_VIEWPORT_WIDTH = 1920;
const DEFAULT_VIEWPORT_HEIGHT = 1080;
const DEFAULT_DISPLAY_MODE = 'Shaded';

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Request failed: ${response.status}`);
  }
  return response.json() as Promise<T>;
}

function parseWebView2Json<T>(value: unknown): T {
  if (typeof value === 'string')
    return JSON.parse(value) as T;
  return value as T;
}

function normalizeViewportDto(viewport: ViewportDto, index: number): ViewportInfo {
  return {
    id: String(index + 1),
    name: viewport.name,
    width: viewport.width && viewport.width > 0 ? viewport.width : DEFAULT_VIEWPORT_WIDTH,
    height: viewport.height && viewport.height > 0 ? viewport.height : DEFAULT_VIEWPORT_HEIGHT,
    displayMode: viewport.displayMode ?? DEFAULT_DISPLAY_MODE,
    isActive: viewport.isActive,
  };
}

function createWebView2Bridge(raw: RhinoBridge): RhinoBridge {
  return {
    CaptureViewport: (projectId, width, height, displayMode) =>
      raw.CaptureViewport(projectId, width, height, displayMode),
    GetDisplayModes: async () => {
      const modes = await raw.GetDisplayModes();
      const parsed = parseWebView2Json<DisplayModeDto[] | string[]>(modes);
      if (Array.isArray(parsed) && parsed.length > 0 && typeof parsed[0] === 'string')
        return parsed as string[];
      return (parsed as DisplayModeDto[]).map((m) => m.name);
    },
    GetViewports: async () => {
      const viewports = await raw.GetViewports();
      const parsed = parseWebView2Json<ViewportDto[]>(viewports);
      return parsed.map((v, index) => normalizeViewportDto(v, index));
    },
    GetApiUrl: () => raw.GetApiUrl(),
    GetActiveDisplayMode: () => raw.GetActiveDisplayMode(),
  };
}

function createHttpBridge(): RhinoBridge {
  return {
    CaptureViewport: async (projectId, width, height, displayMode) => {
      const result = await fetchJson<{ captureId: string }>('/api/rhino/capture', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId, width, height, displayMode }),
      });
      return result.captureId;
    },
    GetDisplayModes: async () => {
      const modes = await fetchJson<DisplayModeDto[]>('/api/rhino/display-modes');
      return modes.map((m) => m.name);
    },
    GetViewports: async () => {
      const viewports = await fetchJson<ViewportDto[]>('/api/rhino/viewports');
      return viewports.map((v, index) => normalizeViewportDto(v, index));
    },
    GetApiUrl: async () => window.location.origin,
    GetActiveDisplayMode: async () => {
      const result = await fetchJson<{ mode: string }>('/api/rhino/active-display-mode');
      return result.mode;
    },
  };
}

/** HTTP bridge polling is implemented by the macOS Rhino plugin only. */
export function supportsHttpBridge(): boolean {
  if (typeof navigator === 'undefined')
    return false;
  return /Mac|iPhone|iPad/i.test(navigator.userAgent);
}

export function detectRhinoRuntime(): RhinoRuntime {
  if (window.chrome?.webview?.hostObjects?.rhino)
    return 'webview2';
  if (supportsHttpBridge())
    return 'http';
  return 'none';
}

export function getRhinoBridge(): RhinoBridge | null {
  const webView2 = window.chrome?.webview?.hostObjects?.rhino;
  if (webView2)
    return createWebView2Bridge(webView2);

  return null;
}

export async function resolveRhinoBridge(): Promise<RhinoBridgeStatus> {
  const webView2 = window.chrome?.webview?.hostObjects?.rhino;
  if (webView2) {
    try {
      const bridge = createWebView2Bridge(webView2);
      await bridge.GetDisplayModes();
      return { runtime: 'webview2', bridge, isAvailable: true };
    } catch (error) {
      return {
        runtime: 'webview2',
        bridge: null,
        isAvailable: false,
        error: error instanceof Error ? error.message : 'WebView2 bridge unavailable',
      };
    }
  }

  if (!supportsHttpBridge()) {
    return {
      runtime: 'none',
      bridge: null,
      isAvailable: false,
      error: 'Rhino bridge requires the docked WebView2 panel on Windows. Open Image Studio from Rhino.',
    };
  }

  try {
    const status = await fetchJson<{ connected: boolean }>('/api/rhino/status');
    if (!status.connected) {
      return { runtime: 'http', bridge: null, isAvailable: false, error: 'Rhino bridge not connected' };
    }
    const bridge = createHttpBridge();
    await bridge.GetDisplayModes();
    return { runtime: 'http', bridge, isAvailable: true };
  } catch (error) {
    return {
      runtime: 'none',
      bridge: null,
      isAvailable: false,
      error: error instanceof Error ? error.message : 'Rhino bridge unavailable',
    };
  }
}

export const mockRhinoBridge: RhinoBridge = {
  CaptureViewport: async () => 'mock-capture-id',
  GetDisplayModes: async () => ['Shaded', 'Rendered'],
  GetViewports: async () => [
    { id: '1', name: 'Perspective', width: 1920, height: 1080, displayMode: 'Shaded' },
  ],
  GetApiUrl: async () => 'http://localhost:17532',
  GetActiveDisplayMode: async () => 'Shaded',
};
