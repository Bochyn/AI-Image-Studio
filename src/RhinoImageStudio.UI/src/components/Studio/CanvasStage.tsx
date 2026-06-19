import { useState, useMemo } from 'react';
import { Button } from '@/components/Common/Button';
import { CompareSlider } from '@/components/Studio/CompareSlider';
import { MaskCanvas } from '@/components/Studio/MaskCanvas';
import { SmoothProgress } from '@/components/Common/SmoothProgress';
import { ZoomIn, ZoomOut, Maximize2, Columns, Download, Sparkles, ImageIcon, Save, ImagePlus, Paintbrush } from 'lucide-react';
import { Job, Capture, Generation } from '@/lib/types';
import type { MaskState } from '@/lib/types';
import { cn } from '@/lib/utils';

interface CompareItem {
  id: string;
  label: string;
  imageUrl: string;
  thumbnailUrl?: string;
  type: 'capture' | 'generation';
}

interface CanvasStageProps {
  currentImage: string | null;
  originalImage?: string | null;
  isProcessing: boolean;
  activeJob?: Job | null;
  onDownload?: () => void;
  supportsReferences?: boolean;
  hasReferences?: boolean;
  onToggleReferences?: () => void;
  captures?: Capture[];
  generations?: Generation[];
  selectedItemId?: string | null;
  maskState?: MaskState;
  onMaskLayerUpdate?: (layerId: string, imageData: ImageData) => void;
  isMaskMode?: boolean;
  onToggleMaskMode?: () => void;
  supportsMasks?: boolean;
  sourceWidth?: number;
  sourceHeight?: number;
  brushSize?: number;
  onBrushSizeChange?: (size: number) => void;
}

export function CanvasStage({
  currentImage,
  originalImage,
  isProcessing,
  activeJob,
  onDownload,
  supportsReferences,
  hasReferences,
  onToggleReferences,
  captures,
  generations,
  selectedItemId,
  maskState,
  onMaskLayerUpdate,
  isMaskMode,
  onToggleMaskMode,
  supportsMasks,
  sourceWidth,
  sourceHeight,
  brushSize,
  onBrushSizeChange,
}: CanvasStageProps) {
  const [zoom, setZoom] = useState(1);
  const [compareMode, setCompareMode] = useState(false);
  const [imageAId, setImageAId] = useState<string | null>(null);
  const [imageBId, setImageBId] = useState<string | null>(null);
  const [compareOpacity, setCompareOpacity] = useState(100);

  const compareItems: CompareItem[] = useMemo(() => {
    const items: CompareItem[] = [];
    (captures || []).forEach(c => {
      if (c.imageUrl) {
        items.push({
          id: c.id,
          label: c.viewName || `Capture ${new Date(c.createdAt).toLocaleTimeString()}`,
          imageUrl: c.imageUrl,
          thumbnailUrl: c.thumbnailUrl || c.imageUrl,
          type: 'capture',
        });
      }
    });
    (generations || []).forEach(g => {
      if (g.imageUrl) {
        items.push({
          id: g.id,
          label: g.prompt?.substring(0, 40) || `Generation ${new Date(g.createdAt).toLocaleTimeString()}`,
          imageUrl: g.imageUrl,
          thumbnailUrl: g.thumbnailUrl || g.imageUrl,
          type: 'generation',
        });
      }
    });
    return items;
  }, [captures, generations]);

  const imageAUrl = compareItems.find(i => i.id === imageAId)?.imageUrl || null;
  const imageBUrl = compareItems.find(i => i.id === imageBId)?.imageUrl || null;

  const handleZoomIn = () => setZoom(z => Math.min(z * 1.5, 8));
  const handleZoomOut = () => setZoom(z => Math.max(z / 1.5, 0.5));
  const handleFit = () => setZoom(1);

  const canCompare = compareItems.length >= 2 || !!(originalImage && currentImage && originalImage !== currentImage);

  // Default selections when entering compare mode
  const handleEnterCompare = () => {
    if (!compareMode) {
      // Turn off mask mode when entering compare
      if (isMaskMode && onToggleMaskMode) onToggleMaskMode();
      // Entering compare mode - set defaults
      if (selectedItemId) {
        setImageBId(selectedItemId);
      }
      if (originalImage) {
        const sourceItem = compareItems.find(i => i.imageUrl === originalImage);
        if (sourceItem) setImageAId(sourceItem.id);
      }
    } else {
      // Exiting compare mode - reset
      setImageAId(null);
      setImageBId(null);
      setCompareOpacity(100);
    }
    setCompareMode(!compareMode);
  };

  return (
    <div className="relative flex-1 h-full w-full flex items-center justify-center overflow-hidden bg-panel border border-border">

      {/* Floating Toolbar */}
      <div className="absolute top-6 right-6 z-50 flex items-center gap-1 p-1 bg-card/80 backdrop-blur-md rounded-full shadow-lg border border-border">
        <Button
          variant="ghost"
          size="icon"
          onClick={handleZoomOut}
          className="h-8 w-8 rounded-full hover:bg-primary/10 text-primary"
          title="Zoom Out"
          aria-label="Zoom out"
        >
          <ZoomOut className="h-4 w-4" />
        </Button>

        <span className="text-xs font-medium text-text w-10 text-center select-none">
          {Math.round(zoom * 100)}%
        </span>

        <Button
          variant="ghost"
          size="icon"
          onClick={handleZoomIn}
          className="h-8 w-8 rounded-full hover:bg-primary/10 text-primary"
          title="Zoom In"
          aria-label="Zoom in"
        >
          <ZoomIn className="h-4 w-4" />
        </Button>

        <div className="w-px h-4 bg-border mx-1" />

        <Button
          variant="ghost"
          size="icon"
          onClick={handleFit}
          className="h-8 w-8 rounded-full hover:bg-primary/10 text-primary"
          title="Fit to Screen"
          aria-label="Fit to screen"
        >
          <Maximize2 className="h-4 w-4" />
        </Button>

        {canCompare && (
          <>
            <div className="w-px h-4 bg-border mx-1" />
            <Button
              variant={compareMode ? 'secondary' : 'ghost'}
              size="icon"
              onClick={handleEnterCompare}
              className={`h-8 w-8 rounded-full transition-colors ${
                compareMode ? 'bg-primary text-background hover:bg-primary/90' : 'hover:bg-primary/10 text-primary'
              }`}
              title="Toggle Compare"
              aria-label="Toggle compare mode"
            >
              <Columns className="h-4 w-4" />
            </Button>
          </>
        )}

        {supportsReferences && (
          <>
            <div className="w-px h-4 bg-border mx-1" />
            <Button
              variant={hasReferences ? 'secondary' : 'ghost'}
              size="icon"
              onClick={onToggleReferences}
              className={`h-8 w-8 rounded-full transition-colors ${
                hasReferences ? 'bg-primary text-background hover:bg-primary/90' : 'hover:bg-primary/10 text-primary'
              }`}
              title="Reference Images"
              aria-label="Toggle reference images panel"
            >
              <ImagePlus className="h-4 w-4" />
            </Button>
          </>
        )}

        {supportsMasks && (
          <>
            <div className="w-px h-4 bg-border mx-1" />
            <Button
              variant={isMaskMode ? 'secondary' : 'ghost'}
              size="icon"
              onClick={onToggleMaskMode}
              className={`h-8 w-8 rounded-full transition-colors ${
                isMaskMode ? 'bg-primary text-background hover:bg-primary/90' : 'hover:bg-primary/10 text-primary'
              }`}
              title="Mask Drawing"
              aria-label="Toggle mask drawing mode"
            >
              <Paintbrush className="h-4 w-4" />
            </Button>
          </>
        )}

        {isMaskMode && (
          <>
            <div className="w-px h-4 bg-border mx-1" />
            <div className="flex items-center gap-1.5 px-1">
              <span className="text-[9px] font-medium text-secondary select-none w-3 text-right">
                {brushSize || 20}
              </span>
              <input
                type="range"
                min={5}
                max={200}
                value={brushSize || 20}
                onChange={(e) => onBrushSizeChange?.(Number(e.target.value))}
                className="w-20 h-1 accent-primary"
                title="Brush Size"
              />
            </div>
          </>
        )}

        {currentImage && (
          <Button
            variant="ghost"
            size="icon"
            onClick={onDownload}
            className="h-8 w-8 rounded-full hover:bg-primary/10 text-primary"
            title="Download"
            aria-label="Download image"
          >
            <Download className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Compare Opacity Control */}
      {compareMode && (
        <div className="absolute top-16 right-6 z-50 flex items-center gap-2 p-2 bg-card/80 backdrop-blur-md rounded-xl shadow-lg border border-border">
          <span className="text-micro font-medium text-secondary uppercase tracking-wide">B Opacity</span>
          <input
            type="range"
            min={0}
            max={100}
            value={compareOpacity}
            onChange={(e) => setCompareOpacity(Number(e.target.value))}
            className="w-24 h-1 accent-primary"
          />
          <span className="text-xs text-primary w-8 text-right">{compareOpacity}%</span>
        </div>
      )}

      {/* Main Canvas Area */}
      <div className="w-full h-full p-8 flex items-center justify-center">

        {isProcessing && (
          <div className="absolute inset-0 z-40 bg-background/80 backdrop-blur-sm flex items-center justify-center rounded-2xl">
            <div className="flex flex-col items-center gap-5 p-8 bg-card rounded-2xl border border-border shadow-2xl min-w-[280px]">
              {/* Animated Icon */}
              <div className="relative">
                <div className="absolute inset-0 bg-primary/20 rounded-full blur-xl animate-pulse" />
                <div className="relative w-16 h-16 rounded-full bg-gradient-to-br from-primary/30 to-primary/10 flex items-center justify-center border border-primary/30">
                  {activeJob && activeJob.progress >= 90 ? (
                    <Save className="h-7 w-7 text-primary animate-pulse" />
                  ) : activeJob && activeJob.progress >= 20 ? (
                    <Sparkles className="h-7 w-7 text-primary animate-pulse" />
                  ) : (
                    <ImageIcon className="h-7 w-7 text-primary animate-pulse" />
                  )}
                </div>
              </div>

              {/* Title */}
              <div className="text-center">
                <h3 className="text-base font-semibold text-primary mb-1">
                  {activeJob?.type === 'Generate' ? 'Generating Image' :
                   activeJob?.type === 'Upscale' ? 'Upscaling Image' :
                   activeJob?.type === 'Refine' ? 'Refining Image' :
                   activeJob?.type === 'MultiAngle' ? 'Rendering 3D View' : 'Processing'}
                </h3>
                <p className="text-xs text-secondary">
                  {activeJob?.progressMessage || 'Starting...'}
                </p>
              </div>

              {/* Progress Bar with smooth animation */}
              <SmoothProgress targetValue={activeJob?.progress ?? 0} />

              {/* Phase Indicators */}
              <div className="flex items-center gap-2 text-micro text-accent">
                <div className={`w-2 h-2 rounded-full transition-colors ${(activeJob?.progress ?? 0) >= 10 ? 'bg-primary' : 'bg-accent/50'}`} />
                <span className={(activeJob?.progress ?? 0) >= 10 && (activeJob?.progress ?? 0) < 20 ? 'text-text' : ''}>Prepare</span>
                <div className="w-4 h-px bg-border" />
                <div className={`w-2 h-2 rounded-full transition-colors ${(activeJob?.progress ?? 0) >= 20 ? 'bg-primary' : 'bg-accent/50'}`} />
                <span className={(activeJob?.progress ?? 0) >= 20 && (activeJob?.progress ?? 0) < 90 ? 'text-text' : ''}>Generate</span>
                <div className="w-4 h-px bg-border" />
                <div className={`w-2 h-2 rounded-full transition-colors ${(activeJob?.progress ?? 0) >= 90 ? 'bg-primary' : 'bg-accent/50'}`} />
                <span className={(activeJob?.progress ?? 0) >= 90 ? 'text-text' : ''}>Save</span>
              </div>
            </div>
          </div>
        )}

        {currentImage ? (
          <div
            className={`relative shadow-2xl transition-transform duration-200 ease-out origin-center ${
              !compareMode && 'rounded-sm overflow-hidden ring-1 ring-border'
            }`}
            style={{
              transform: `scale(${zoom})`,
              maxWidth: '100%',
              maxHeight: '100%',
            }}
          >
            {compareMode ? (
              <div className="flex flex-col items-center gap-3 max-w-full">
                {/* Labels */}
                {imageAUrl && imageBUrl && (
                  <div className="flex items-center justify-between w-full px-1">
                    <span className="text-xs font-medium text-secondary uppercase tracking-wide">
                      A: {compareItems.find(i => i.id === imageAId)?.label || 'Image A'}
                    </span>
                    <span className="text-xs font-medium text-secondary uppercase tracking-wide">
                      B: {compareItems.find(i => i.id === imageBId)?.label || 'Image B'}
                    </span>
                  </div>
                )}

                {/* Slider */}
                {imageAUrl && imageBUrl ? (
                  <CompareSlider
                    leftImage={imageAUrl}
                    rightImage={imageBUrl}
                    opacity={compareOpacity}
                    className="rounded-sm shadow-2xl"
                  />
                ) : (
                  <div className="flex flex-col items-center justify-center py-16 text-secondary opacity-50">
                    <Columns className="h-8 w-8 mb-2 opacity-30" />
                    <p className="text-sm">Select Image A and Image B below</p>
                  </div>
                )}

                {/* Thumbnail Selector Rows */}
                <div className="w-full max-w-[600px] flex flex-col gap-2 mt-1">
                  {/* Row A */}
                  <div className="flex items-center gap-2">
                    <span className="text-micro font-semibold text-primary uppercase tracking-wider w-4 shrink-0">A</span>
                    <div className="flex gap-1.5 overflow-x-auto pb-1 custom-scrollbar">
                      {compareItems.map(item => (
                        <button
                          key={item.id}
                          onClick={() => setImageAId(item.id)}
                          className={cn(
                            "relative shrink-0 w-12 h-12 rounded-lg overflow-hidden border-2 transition-all",
                            imageAId === item.id
                              ? "border-primary ring-1 ring-primary/50"
                              : "border-transparent hover:border-border opacity-70 hover:opacity-100"
                          )}
                          title={item.label}
                        >
                          <img
                            src={item.thumbnailUrl || item.imageUrl}
                            alt={item.label}
                            className="w-full h-full object-cover"
                            draggable={false}
                          />
                          <span className="absolute bottom-0 left-0 right-0 bg-background/70 text-[8px] text-center text-primary font-medium leading-tight py-px">
                            {item.type === 'capture' ? 'C' : 'G'}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Row B */}
                  <div className="flex items-center gap-2">
                    <span className="text-micro font-semibold text-primary uppercase tracking-wider w-4 shrink-0">B</span>
                    <div className="flex gap-1.5 overflow-x-auto pb-1 custom-scrollbar">
                      {compareItems.map(item => (
                        <button
                          key={item.id}
                          onClick={() => setImageBId(item.id)}
                          className={cn(
                            "relative shrink-0 w-12 h-12 rounded-lg overflow-hidden border-2 transition-all",
                            imageBId === item.id
                              ? "border-primary ring-1 ring-primary/50"
                              : "border-transparent hover:border-border opacity-70 hover:opacity-100"
                          )}
                          title={item.label}
                        >
                          <img
                            src={item.thumbnailUrl || item.imageUrl}
                            alt={item.label}
                            className="w-full h-full object-cover"
                            draggable={false}
                          />
                          <span className="absolute bottom-0 left-0 right-0 bg-background/70 text-[8px] text-center text-primary font-medium leading-tight py-px">
                            {item.type === 'capture' ? 'C' : 'G'}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              <>
                <img
                  src={currentImage}
                  alt="Current"
                  className="max-w-full max-h-[85vh] w-auto h-auto object-contain rounded-sm shadow-2xl"
                  draggable={false}
                />
                {isMaskMode && maskState && sourceWidth && sourceHeight && (
                  <MaskCanvas
                    sourceWidth={sourceWidth}
                    sourceHeight={sourceHeight}
                    zoom={zoom}
                    layers={maskState.layers}
                    activeLayerId={maskState.activeLayerId}
                    brush={maskState.brush}
                    isActive={isMaskMode}
                    onLayerUpdate={onMaskLayerUpdate!}
                  />
                )}
              </>
            )}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center text-secondary opacity-50 select-none">
            <div className="w-24 h-24 mb-6 rounded-2xl bg-primary/5 border border-border/50 flex items-center justify-center">
              <Maximize2 className="h-10 w-10 opacity-20" />
            </div>
            <p className="text-lg font-medium">No image selected</p>
            <p className="text-sm opacity-60 mt-1">Select a capture or start generating</p>
          </div>
        )}
      </div>
    </div>
  );
}
