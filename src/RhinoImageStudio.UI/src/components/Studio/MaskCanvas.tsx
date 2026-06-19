// ============================================================================
// MaskCanvas — Canvas stack for mask drawing with layer compositing
// Overlays on top of the image in CanvasStage.
// Display canvas (all layers composited) + cursor canvas (brush preview).
// ============================================================================

import React, { useRef, useEffect, useCallback, useState } from 'react';
import { BrushEngine } from '../../lib/BrushEngine';
import { MaskHistory } from '../../lib/MaskHistory';
import type { MaskLayer, BrushSettings } from '../../lib/types';

export interface MaskCanvasProps {
  /** Source image width in pixels (native resolution). */
  sourceWidth: number;
  /** Source image height in pixels (native resolution). */
  sourceHeight: number;
  /** Current zoom level from CanvasStage (1 = 100%). */
  zoom: number;
  /** All mask layers. */
  layers: MaskLayer[];
  /** ID of the currently active layer (receives drawing input). */
  activeLayerId: string | null;
  /** Current brush settings. */
  brush: BrushSettings;
  /** When false, layers render as overlay only — no drawing. */
  isActive: boolean;
  /** Callback when a layer's pixel data changes (after stroke ends). */
  onLayerUpdate: (layerId: string, imageData: ImageData) => void;
}

export function MaskCanvas({
  sourceWidth,
  sourceHeight,
  zoom: _zoom,
  layers,
  activeLayerId,
  brush,
  isActive,
  onLayerUpdate,
}: MaskCanvasProps) {
  // _zoom is received for API completeness but not consumed directly here.
  // The parent container (CanvasStage) applies zoom via CSS transform: scale().
  void _zoom;
  // --- Refs ---
  const displayCanvasRef = useRef<HTMLCanvasElement>(null);
  const cursorCanvasRef = useRef<HTMLCanvasElement>(null);

  // Per-layer offscreen canvases, keyed by layer ID
  const offscreenCanvasesRef = useRef<Map<string, HTMLCanvasElement>>(new Map());
  // Per-layer BrushEngine instances
  const brushEnginesRef = useRef<Map<string, BrushEngine>>(new Map());
  // Per-layer MaskHistory instances
  const historiesRef = useRef<Map<string, MaskHistory>>(new Map());

  // Drawing state
  const isDrawingRef = useRef(false);
  const [cursorPos, setCursorPos] = useState<{ x: number; y: number } | null>(null);

  // --- Helpers: get or create offscreen canvas / engine / history for a layer ---

  const getOffscreenCanvas = useCallback(
    (layerId: string): HTMLCanvasElement => {
      const map = offscreenCanvasesRef.current;
      let canvas = map.get(layerId);
      if (!canvas || canvas.width !== sourceWidth || canvas.height !== sourceHeight) {
        canvas = document.createElement('canvas');
        canvas.width = sourceWidth;
        canvas.height = sourceHeight;
        map.set(layerId, canvas);
      }
      return canvas;
    },
    [sourceWidth, sourceHeight]
  );

  const getBrushEngine = useCallback(
    (layerId: string): BrushEngine => {
      const map = brushEnginesRef.current;
      const existingEngine = map.get(layerId);
      const canvas = getOffscreenCanvas(layerId);
      const ctx = canvas.getContext('2d')!;
      // Recreate engine if canvas was recreated (stale context) or doesn't exist yet
      if (!existingEngine || existingEngine.getContext() !== ctx) {
        const engine = new BrushEngine(ctx);
        map.set(layerId, engine);
        return engine;
      }
      return existingEngine;
    },
    [getOffscreenCanvas]
  );

  const getHistory = useCallback((layerId: string): MaskHistory => {
    const map = historiesRef.current;
    let history = map.get(layerId);
    if (!history) {
      // Use 20 for 1K, 10 for 4K. Threshold at ~2048px.
      const maxSteps = sourceWidth * sourceHeight > 2048 * 2048 ? 10 : 20;
      history = new MaskHistory(maxSteps);
      map.set(layerId, history);
    }
    return history;
  }, [sourceWidth, sourceHeight]);

  // --- Sync layer imageData into offscreen canvases ---

  useEffect(() => {
    for (const layer of layers) {
      const canvas = getOffscreenCanvas(layer.id);
      const ctx = canvas.getContext('2d')!;
      if (layer.imageData) {
        ctx.putImageData(layer.imageData, 0, 0);
      } else {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
      }
    }

    // Clean up offscreen canvases for removed layers
    const layerIds = new Set(layers.map((l) => l.id));
    for (const [id] of offscreenCanvasesRef.current) {
      if (!layerIds.has(id)) {
        offscreenCanvasesRef.current.delete(id);
        brushEnginesRef.current.delete(id);
        historiesRef.current.delete(id);
      }
    }
  }, [layers, getOffscreenCanvas]);

  // --- Composite all layers onto the display canvas ---

  const compositeDisplay = useCallback(() => {
    const displayCanvas = displayCanvasRef.current;
    if (!displayCanvas) return;

    const ctx = displayCanvas.getContext('2d')!;
    ctx.clearRect(0, 0, sourceWidth, sourceHeight);

    for (const layer of layers) {
      if (!layer.visible) continue;

      const offscreen = offscreenCanvasesRef.current.get(layer.id);
      if (!offscreen) continue;

      // Active layer at 40% opacity, inactive at 20%
      const isActiveLayer = layer.id === activeLayerId;
      ctx.globalAlpha = isActiveLayer ? 0.4 : 0.2;
      ctx.drawImage(offscreen, 0, 0);
    }

    ctx.globalAlpha = 1.0;
  }, [layers, activeLayerId, sourceWidth, sourceHeight]);

  // Re-composite whenever layers, visibility, or active layer changes
  useEffect(() => {
    compositeDisplay();
  }, [compositeDisplay]);

  // --- Coordinate mapping: screen (display) -> source resolution ---

  const screenToSource = useCallback(
    (clientX: number, clientY: number): { x: number; y: number } | null => {
      const displayCanvas = displayCanvasRef.current;
      if (!displayCanvas) return null;

      const rect = displayCanvas.getBoundingClientRect();
      const displayWidth = rect.width;
      const displayHeight = rect.height;

      const mouseX = clientX - rect.left;
      const mouseY = clientY - rect.top;

      // Map from display pixels to source pixels
      const srcX = mouseX * (sourceWidth / displayWidth);
      const srcY = mouseY * (sourceHeight / displayHeight);

      return { x: srcX, y: srcY };
    },
    [sourceWidth, sourceHeight]
  );

  // --- Drawing event handlers ---

  const handlePointerDown = useCallback(
    (e: React.PointerEvent<HTMLCanvasElement>) => {
      if (!isActive || !activeLayerId) return;

      // Capture pointer for continuous drawing even outside canvas bounds
      e.currentTarget.setPointerCapture(e.pointerId);

      const pos = screenToSource(e.clientX, e.clientY);
      if (!pos) return;

      isDrawingRef.current = true;

      const engine = getBrushEngine(activeLayerId);
      const activeLayer = layers.find((l) => l.id === activeLayerId);

      engine.color = activeLayer?.color || '#ef4444';
      engine.size = brush.size;
      engine.mode = brush.mode;

      // Save state before this stroke for undo
      const history = getHistory(activeLayerId);
      history.pushState(engine.getImageData());

      engine.beginStroke(pos.x, pos.y);
      compositeDisplay();
    },
    [isActive, activeLayerId, layers, brush, screenToSource, getBrushEngine, getHistory, compositeDisplay]
  );

  const handlePointerMove = useCallback(
    (e: React.PointerEvent<HTMLCanvasElement>) => {
      // Always update cursor position for brush preview
      const pos = screenToSource(e.clientX, e.clientY);
      if (pos) {
        setCursorPos(pos);
      }

      if (!isDrawingRef.current || !activeLayerId) return;

      if (!pos) return;

      const engine = getBrushEngine(activeLayerId);
      engine.continueStroke(pos.x, pos.y);
      compositeDisplay();
    },
    [activeLayerId, screenToSource, getBrushEngine, compositeDisplay]
  );

  const handlePointerUp = useCallback(
    (e: React.PointerEvent<HTMLCanvasElement>) => {
      if (!isDrawingRef.current || !activeLayerId) return;

      e.currentTarget.releasePointerCapture(e.pointerId);

      const engine = getBrushEngine(activeLayerId);
      engine.endStroke();

      isDrawingRef.current = false;

      // Notify parent of the updated layer data
      const imageData = engine.getImageData();
      onLayerUpdate(activeLayerId, imageData);
      compositeDisplay();
    },
    [activeLayerId, getBrushEngine, onLayerUpdate, compositeDisplay]
  );

  const handlePointerLeave = useCallback(() => {
    setCursorPos(null);
  }, []);

  // --- Cursor canvas: brush preview circle ---

  useEffect(() => {
    const cursorCanvas = cursorCanvasRef.current;
    if (!cursorCanvas) return;

    const ctx = cursorCanvas.getContext('2d')!;
    ctx.clearRect(0, 0, sourceWidth, sourceHeight);

    if (!cursorPos || !isActive) return;

    const activeLayer = layers.find((l) => l.id === activeLayerId);
    const color = activeLayer?.color || '#ffffff';

    ctx.beginPath();
    ctx.arc(cursorPos.x, cursorPos.y, brush.size / 2, 0, Math.PI * 2);
    ctx.strokeStyle = color;
    ctx.lineWidth = 2 * (sourceWidth / (displayCanvasRef.current?.getBoundingClientRect().width || sourceWidth));
    ctx.globalAlpha = 0.8;
    ctx.stroke();

    // Crosshair at center
    const crossSize = 4 * (sourceWidth / (displayCanvasRef.current?.getBoundingClientRect().width || sourceWidth));
    ctx.beginPath();
    ctx.moveTo(cursorPos.x - crossSize, cursorPos.y);
    ctx.lineTo(cursorPos.x + crossSize, cursorPos.y);
    ctx.moveTo(cursorPos.x, cursorPos.y - crossSize);
    ctx.lineTo(cursorPos.x, cursorPos.y + crossSize);
    ctx.strokeStyle = color;
    ctx.lineWidth = 1 * (sourceWidth / (displayCanvasRef.current?.getBoundingClientRect().width || sourceWidth));
    ctx.globalAlpha = 0.5;
    ctx.stroke();

    ctx.globalAlpha = 1.0;
  }, [cursorPos, brush.size, isActive, activeLayerId, layers, sourceWidth, sourceHeight]);

  // --- Keyboard shortcuts for undo/redo ---

  useEffect(() => {
    if (!isActive || !activeLayerId) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      const isCtrl = e.ctrlKey || e.metaKey;

      if (isCtrl && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        const history = getHistory(activeLayerId);
        const state = history.undo();
        const engine = getBrushEngine(activeLayerId);
        if (state) {
          engine.putImageData(state);
        } else {
          engine.clear();
        }
        onLayerUpdate(activeLayerId, engine.getImageData());
        compositeDisplay();
      }

      if (isCtrl && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) {
        e.preventDefault();
        const history = getHistory(activeLayerId);
        const state = history.redo();
        if (state) {
          const engine = getBrushEngine(activeLayerId);
          engine.putImageData(state);
          onLayerUpdate(activeLayerId, engine.getImageData());
          compositeDisplay();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isActive, activeLayerId, getHistory, getBrushEngine, onLayerUpdate, compositeDisplay]);

  // --- Canvas display styling ---
  // Canvas HTML attrs are set to source resolution for full-res drawing.
  // CSS width/height: 100% matches the parent container (sized by <img>).
  // Zoom is handled by the parent container via transform: scale(zoom).

  const displayStyle: React.CSSProperties = {
    width: '100%',
    height: '100%',
    position: 'absolute',
    top: 0,
    left: 0,
  };

  return (
    <div
      className="absolute inset-0 pointer-events-none"
    >
      {/* Display canvas: all layers composited */}
      <canvas
        ref={displayCanvasRef}
        width={sourceWidth}
        height={sourceHeight}
        style={{
          ...displayStyle,
          pointerEvents: isActive ? 'auto' : 'none',
          cursor: isActive ? 'none' : 'default',
        }}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerLeave}
      />

      {/* Cursor canvas: brush preview (always on top) */}
      <canvas
        ref={cursorCanvasRef}
        width={sourceWidth}
        height={sourceHeight}
        style={{
          ...displayStyle,
          pointerEvents: 'none', // Pass-through — drawing happens on display canvas
        }}
      />
    </div>
  );
}
