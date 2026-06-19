import { useCallback, useEffect, useState } from 'react';
import { resolveRhinoBridge, type RhinoBridgeStatus, type RhinoRuntime } from '@/lib/rhino';
import type { RhinoBridge, ViewportInfo } from '@/lib/types';

const INITIAL_POLL_MS = 1_000;
const MAX_POLL_MS = 8_000;

export function useRhino() {
  const [status, setStatus] = useState<RhinoBridgeStatus>({
    runtime: 'none',
    bridge: null,
    isAvailable: false,
  });
  const [displayModes, setDisplayModes] = useState<string[]>([]);
  const [viewports, setViewports] = useState<ViewportInfo[]>([]);

  const loadBridgeData = useCallback(async (bridge: RhinoBridge) => {
    const [modes, vps] = await Promise.all([
      bridge.GetDisplayModes(),
      bridge.GetViewports(),
    ]);
    setDisplayModes(modes);
    setViewports(vps);
  }, []);

  useEffect(() => {
    let cancelled = false;
    let pollTimer: ReturnType<typeof setTimeout> | undefined;
    let pollDelay = INITIAL_POLL_MS;

    const scheduleReconnect = () => {
      if (cancelled)
        return;
      pollTimer = setTimeout(() => {
        void connect();
      }, pollDelay);
      pollDelay = Math.min(pollDelay * 2, MAX_POLL_MS);
    };

    const connect = async () => {
      const next = await resolveRhinoBridge();
      if (cancelled)
        return;

      setStatus(next);

      if (!next.bridge) {
        if (next.runtime === 'http' && !next.isAvailable) {
          scheduleReconnect();
        }
        return;
      }

      pollDelay = INITIAL_POLL_MS;

      try {
        await loadBridgeData(next.bridge);
      } catch (error) {
        if (!cancelled) {
          setStatus({
            ...next,
            isAvailable: false,
            error: error instanceof Error ? error.message : 'Failed to query Rhino bridge',
          });
          scheduleReconnect();
        }
      }
    };

    void connect();

    return () => {
      cancelled = true;
      if (pollTimer)
        clearTimeout(pollTimer);
    };
  }, [loadBridgeData]);

  const retry = useCallback(async () => {
    const next = await resolveRhinoBridge();
    setStatus(next);
    if (next.bridge) {
      try {
        await loadBridgeData(next.bridge);
      } catch (error) {
        setStatus({
          ...next,
          isAvailable: false,
          error: error instanceof Error ? error.message : 'Failed to query Rhino bridge',
        });
      }
    }
  }, [loadBridgeData]);

  return {
    rhino: status.bridge,
    runtime: status.runtime as RhinoRuntime,
    isAvailable: status.isAvailable,
    error: status.error,
    viewports,
    displayModes,
    retry,
  };
}

export type { RhinoBridge };
