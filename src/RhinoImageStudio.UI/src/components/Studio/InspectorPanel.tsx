import { useState, useEffect } from 'react';
import { Capture, Generation, Job } from '@/lib/types';
import type { MaskState } from '@/lib/types';
import { Button } from '@/components/Common/Button';
import {
  Sparkles,
  Loader2,
  Wand2,
  Settings2,
  RotateCcw,
  Paintbrush,
  Eye,
  EyeOff,
  Trash2,
  Plus
} from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  ModeType,
  AVAILABLE_MODELS,
  MODELS,
  AllModelSettings,
  DEFAULT_ALL_SETTINGS,
  TOPAZ_MODELS_LIST,
  MULTI_ANGLE_PRESETS,
  horizontalUiToApi,
  formatHorizontalAngle,
  formatVerticalAngle,
  formatZoom,
} from '@/lib/models';
import { AngleSlider } from '@/components/Common/AngleSlider';
import { InspectorModeSelector } from '@/components/Studio/Inspector/ModeSelector';
import { InspectorModelSelector } from '@/components/Studio/Inspector/ModelSelector';

/** Parse stored generation parameters for settings restoration */
function parseGenerationParams(gen: Generation | null): {
  aspectRatio?: string;
  resolution?: string;
  numImages?: number;
  quality?: string;
  inputFidelity?: string;
} | null {
  if (!gen?.parametersJson) return null;
  try {
    return JSON.parse(gen.parametersJson);
  } catch {
    return null;
  }
}

interface InspectorPanelProps {
  selectedCapture: Capture | null;
  selectedGeneration: Generation | null;
  onGenerate: (prompt: string, settings: any) => void;
  onSettingsChange?: (settings: AllModelSettings, modelId: string) => void;
  jobs: Job[];
  maskState?: MaskState;
  onAddMaskLayer?: () => void;
  onRemoveMaskLayer?: (layerId: string) => void;
  onSelectMaskLayer?: (layerId: string) => void;
  onUpdateMaskInstruction?: (layerId: string, instruction: string) => void;
  onToggleMaskVisibility?: (layerId: string) => void;
  isMaskMode?: boolean;
  onToggleMaskMode?: () => void;
  maxMaskLayers?: number;
  supportsMasks?: boolean;
}

export function InspectorPanel({
  selectedCapture,
  selectedGeneration,
  onGenerate,
  onSettingsChange,
  jobs,
  maskState,
  onAddMaskLayer,
  onRemoveMaskLayer,
  onSelectMaskLayer,
  onUpdateMaskInstruction,
  onToggleMaskVisibility,
  isMaskMode,
  onToggleMaskMode,
  maxMaskLayers,
  supportsMasks,
}: InspectorPanelProps) {
  const [mode, setMode] = useState<ModeType>('generate');
  const [prompt, setPrompt] = useState('');
  const [selectedModelId, setSelectedModelId] = useState<string>('');

  // Unified settings state
  const [settings, setSettings] = useState<AllModelSettings>(DEFAULT_ALL_SETTINGS);

  const activeJobs = jobs.filter(j =>
    j.status === 'Running' || j.status === 'Queued'
  );
  const isProcessing = activeJobs.length > 0;
  const hasSource = !!(selectedCapture || selectedGeneration);

  // Get model-specific options
  const currentModel = MODELS[selectedModelId];
  const availableAspectRatios = currentModel?.aspectRatios || [];
  const availableResolutions = currentModel?.resolutions || [];
  const hasResolutions = availableResolutions.length > 0;

  // Initialize model selection when mode changes
  useEffect(() => {
    const models = AVAILABLE_MODELS[mode];
    if (models.length > 0 && !models.includes(selectedModelId)) {
      setSelectedModelId(models[0]);
    }
  }, [mode]);

  // Notify parent of settings changes
  useEffect(() => {
    onSettingsChange?.(settings, selectedModelId);
  }, [settings, selectedModelId, onSettingsChange]);

  // Validate AR/Resolution when model changes
  useEffect(() => {
    const model = MODELS[selectedModelId];
    if (!model) return;

    const currentAR = settings.generation.aspectRatio;
    const currentRes = settings.generation.resolution;

    // Check if current AR is available for this model
    const arValid = model.aspectRatios?.some(a => a.value === currentAR);
    // Check if current Resolution is available for this model
    const resValid = !model.resolutions || model.resolutions.some(r => r.value === currentRes);

    if (!arValid || !resValid) {
      setSettings(s => ({
        ...s,
        generation: {
          ...s.generation,
          aspectRatio: arValid ? currentAR : (model.aspectRatios?.[0]?.value || '1:1'),
          resolution: resValid ? currentRes : (model.resolutions?.[0]?.value || '1K'),
        }
      }));
    }
  }, [selectedModelId]);

  // Restore full context when selecting a generation from history
  useEffect(() => {
    if (selectedGeneration) {
      // Restore prompt
      setPrompt(selectedGeneration.prompt || '');

      // Restore model (if available and valid for current mode)
      if (selectedGeneration.modelId) {
        const models = AVAILABLE_MODELS[mode];
        if (models.includes(selectedGeneration.modelId)) {
          setSelectedModelId(selectedGeneration.modelId);
        }
      }

      // Restore generation settings (AR, resolution) from parametersJson
      const params = parseGenerationParams(selectedGeneration);
      if (params) {
        setSettings(s => ({
          ...s,
          generation: {
            ...s.generation,
            ...(params.aspectRatio && { aspectRatio: params.aspectRatio }),
            ...(params.resolution && { resolution: params.resolution }),
            ...(params.numImages && { numImages: params.numImages }),
            ...(params.quality && { quality: params.quality }),
            ...(params.inputFidelity && { inputFidelity: params.inputFidelity }),
          }
        }));
      }

      // Restore multi-angle settings
      if (selectedGeneration.azimuth !== undefined || selectedGeneration.elevation !== undefined) {
        setSettings(s => ({
          ...s,
          multiAngle: {
            ...s.multiAngle,
            ...(selectedGeneration.azimuth !== undefined && { horizontalAngle: selectedGeneration.azimuth }),
            ...(selectedGeneration.elevation !== undefined && { verticalAngle: selectedGeneration.elevation }),
            ...(selectedGeneration.zoom !== undefined && { zoom: selectedGeneration.zoom }),
          }
        }));
      }
    } else if (selectedCapture) {
      // Clicking a capture clears the prompt
      setPrompt('');
    }
  }, [selectedGeneration?.id, selectedCapture?.id]);

  const handleSubmit = () => {
    if (!hasSource) return;

    // Construct the payload based on mode and current settings
    let payload: any = {
      mode,  // Pass mode so StudioPage knows which API to call
      model: selectedModelId
    };

    if (mode === 'generate' || mode === 'refine') {
      payload = { ...payload, ...settings.generation };
    } else if (mode === 'multiangle') {
      // Convert UI horizontal angle (-180..+180) to API angle (0..360)
      payload = {
        ...payload,
        horizontalAngle: horizontalUiToApi(settings.multiAngle.horizontalAngle),
        verticalAngle: settings.multiAngle.verticalAngle,
        zoom: settings.multiAngle.zoom,
        loraScale: settings.multiAngle.loraScale,
      };
    } else if (mode === 'upscale') {
      payload = { ...payload, ...settings.upscale };
    }

    onGenerate(prompt, payload);
  };

  return (
    <div className="h-full flex flex-col bg-panel rounded-2xl border border-border overflow-hidden">
      {/* Header */}
      <div className="p-4 border-b border-border/50 bg-background/50">
        <h2 className="text-sm font-semibold tracking-wide text-primary uppercase flex items-center gap-2">
          <Settings2 className="h-4 w-4 opacity-50" />
          Editor
        </h2>
      </div>

      {/* Scrollable Content */}
      <div className="flex-1 overflow-y-auto p-4 custom-scrollbar space-y-6">

        <InspectorModeSelector mode={mode} onChange={setMode} />

        {/* Source Card */}
        {hasSource ? (
          <div className="p-3 bg-card rounded-xl border border-border flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-background/50 overflow-hidden flex-shrink-0">
               <img
                 src={selectedGeneration?.thumbnailUrl || selectedCapture?.thumbnailUrl || selectedGeneration?.imageUrl || selectedCapture?.imageUrl}
                 className="w-full h-full object-cover"
                 alt="Source"
               />
            </div>
            <div className="min-w-0">
              <p className="text-micro font-bold text-accent uppercase">Source Input</p>
              <p className="text-xs text-primary truncate font-medium">
                {selectedGeneration ? 'Generated Image' : 'Rhino Viewport Capture'}
              </p>
            </div>
          </div>
        ) : (
          <div className="p-4 rounded-xl border border-dashed border-border text-center">
            <p className="text-xs text-accent">Select a capture to begin</p>
          </div>
        )}

        <InspectorModelSelector
          mode={mode}
          selectedModelId={selectedModelId}
          onChange={setSelectedModelId}
        />

        <hr className="border-border/50" />

        {/* Dynamic Fields */}
        <div className="space-y-4 animate-in fade-in slide-in-from-bottom-4 duration-500">

          {/* Prompt Input (Generate/Refine only) */}
          {(mode === 'generate' || mode === 'refine') && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-micro font-bold text-accent uppercase tracking-wider">
                  Prompt
                </label>
                <button className="text-micro text-primary hover:underline flex items-center gap-1">
                  <Wand2 className="h-3 w-3" /> Enhance
                </button>
              </div>
              <textarea
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder={mode === 'generate' ? "Describe the image..." : "Describe changes..."}
                className="w-full h-24 bg-card border border-border rounded-xl p-3 text-sm text-primary placeholder:text-accent/50 resize-none focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          )}

          {/* Mask Layers Section */}
          {supportsMasks && hasSource && (mode === 'generate' || mode === 'refine') && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-micro font-bold text-accent uppercase tracking-wider">
                  Mask Layers
                </label>
                <div className="flex items-center gap-1">
                  <button
                    onClick={onToggleMaskMode}
                    className={cn(
                      "flex items-center gap-1 text-micro px-2 py-1 rounded-md transition-colors",
                      isMaskMode
                        ? "bg-primary/20 text-primary"
                        : "text-secondary hover:text-primary"
                    )}
                  >
                    <Paintbrush className="h-3 w-3" />
                    {isMaskMode ? 'Drawing' : 'Draw'}
                  </button>
                  <button
                    onClick={onAddMaskLayer}
                    disabled={!maxMaskLayers || (maskState?.layers.length ?? 0) >= maxMaskLayers}
                    className="flex items-center gap-1 text-micro text-primary hover:text-primary/80 disabled:opacity-30 disabled:cursor-not-allowed px-2 py-1"
                  >
                    <Plus className="h-3 w-3" />
                    Add
                  </button>
                </div>
              </div>

              {/* Layer list */}
              {maskState && maskState.layers.length > 0 ? (
                <div className="space-y-1.5">
                  {maskState.layers.map((layer) => (
                    <div
                      key={layer.id}
                      onClick={() => onSelectMaskLayer?.(layer.id)}
                      className={cn(
                        "p-2 rounded-lg border cursor-pointer transition-all",
                        maskState.activeLayerId === layer.id
                          ? "border-primary bg-primary/5"
                          : "border-border hover:border-border/80"
                      )}
                    >
                      <div className="flex items-center gap-2 mb-1.5">
                        <div className="relative w-3.5 h-3.5 flex-shrink-0">
                          <div
                            className="w-3 h-3 rounded-full"
                            style={{ backgroundColor: layer.color }}
                          />
                          <div className={cn(
                            "absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full border border-panel",
                            layer.imageData && layer.instruction.trim()
                              ? "bg-accent"
                              : "bg-secondary"
                          )} />
                        </div>
                        <span className="text-micro font-medium text-primary flex-1 truncate">
                          {layer.name}
                        </span>
                        <button
                          onClick={(e) => { e.stopPropagation(); onToggleMaskVisibility?.(layer.id); }}
                          className="text-secondary hover:text-primary p-0.5"
                        >
                          {layer.visible ? <Eye className="h-3 w-3" /> : <EyeOff className="h-3 w-3" />}
                        </button>
                        <button
                          onClick={(e) => { e.stopPropagation(); onRemoveMaskLayer?.(layer.id); }}
                          className="text-secondary hover:text-danger p-0.5"
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      </div>
                      <input
                        type="text"
                        value={layer.instruction}
                        onChange={(e) => onUpdateMaskInstruction?.(layer.id, e.target.value)}
                        onClick={(e) => e.stopPropagation()}
                        placeholder="Instruction for this mask..."
                        className="w-full text-[11px] bg-card border border-border rounded-md px-2 py-1.5 text-primary placeholder:text-accent/40 focus:outline-none focus:ring-1 focus:ring-primary"
                      />
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-micro text-secondary italic">
                  No masks yet. Click "Add" to create a mask layer.
                </p>
              )}
            </div>
          )}

          {/* General Generation Settings */}
          {(mode === 'generate' || mode === 'refine') && (
            <>
              {/* Aspect Ratio Selector - dynamic from model */}
              {availableAspectRatios.length > 0 && (
                <div className="space-y-1.5">
                  <label className="text-micro font-bold text-accent uppercase tracking-wider">Aspect Ratio</label>
                  <div className="grid grid-cols-5 gap-1">
                    {availableAspectRatios.map(({ value, label }) => (
                      <button
                        key={value}
                        onClick={() => setSettings(s => ({ ...s, generation: { ...s.generation, aspectRatio: value } }))}
                        title={value}
                        className={cn(
                          "py-2 px-1 text-xs rounded-lg border transition-all",
                          settings.generation.aspectRatio === value
                            ? "bg-primary/20 border-primary text-primary"
                            : "bg-transparent border-border text-secondary hover:border-secondary"
                        )}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Resolution Selector - only if model supports it */}
              {hasResolutions && (
                <div className="space-y-1.5">
                  <label className="text-micro font-bold text-accent uppercase tracking-wider">Resolution</label>
                  <div className="flex gap-2">
                    {availableResolutions.map(({ value, label }) => (
                      <button
                        key={value}
                        onClick={() => setSettings(s => ({ ...s, generation: { ...s.generation, resolution: value } }))}
                        className={cn(
                          "flex-1 py-2 text-xs rounded-lg border transition-all",
                          settings.generation.resolution === value
                            ? "bg-primary/20 border-primary text-primary"
                            : "bg-transparent border-border text-secondary hover:border-secondary"
                        )}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Quality Selector - only if model supports it */}
              {currentModel?.qualityOptions && (
                <div className="space-y-1.5">
                  <label className="text-micro font-bold text-accent uppercase tracking-wider">Quality</label>
                  <div className="flex gap-2">
                    {currentModel.qualityOptions.map(({ value, label }) => (
                      <button
                        key={value}
                        onClick={() => setSettings(s => ({ ...s, generation: { ...s.generation, quality: value } }))}
                        className={cn(
                          "flex-1 py-2 text-xs rounded-lg border transition-all",
                          (settings.generation.quality || 'high') === value
                            ? "bg-primary/20 border-primary text-primary"
                            : "bg-transparent border-border text-secondary hover:border-secondary"
                        )}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Input Fidelity Selector - only if model supports it */}
              {currentModel?.fidelityOptions && (
                <div className="space-y-1.5">
                  <label className="text-micro font-bold text-accent uppercase tracking-wider">Input Fidelity</label>
                  <div className="flex gap-2">
                    {currentModel.fidelityOptions.map(({ value, label }) => (
                      <button
                        key={value}
                        onClick={() => setSettings(s => ({ ...s, generation: { ...s.generation, inputFidelity: value } }))}
                        className={cn(
                          "flex-1 py-2 text-xs rounded-lg border transition-all",
                          (settings.generation.inputFidelity || 'high') === value
                            ? "bg-primary/20 border-primary text-primary"
                            : "bg-transparent border-border text-secondary hover:border-secondary"
                        )}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              <div className="space-y-1.5">
                 <label className="text-micro font-bold text-accent uppercase tracking-wider">
                   Image Count ({settings.generation.numImages})
                 </label>
                 <input
                   type="range"
                   min="1"
                   max="4"
                   step="1"
                   value={settings.generation.numImages}
                   onChange={(e) => setSettings(s => ({ ...s, generation: { ...s.generation, numImages: parseInt(e.target.value) } }))}
                   className="w-full accent-primary bg-primary/10 h-1 rounded-full appearance-none cursor-pointer"
                 />
                 <div className="flex justify-between text-micro text-accent px-1">
                   <span>1</span><span>4</span>
                 </div>
              </div>
            </>
          )}

          {/* Multi-Angle Settings */}
          {mode === 'multiangle' && (
            <div className="space-y-5">
              {/* Presets */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="text-micro font-bold text-accent uppercase tracking-wider">
                    Quick Presets
                  </label>
                  <button
                    onClick={() => setSettings(s => ({
                      ...s,
                      multiAngle: {
                        ...s.multiAngle,
                        horizontalAngle: 0,
                        verticalAngle: 0,
                        zoom: 5,
                      }
                    }))}
                    className="flex items-center gap-1 text-micro text-secondary hover:text-primary transition-colors"
                    title="Reset to defaults"
                  >
                    <RotateCcw className="h-3 w-3" />
                    Reset
                  </button>
                </div>
                <div className="grid grid-cols-4 gap-1.5">
                  {MULTI_ANGLE_PRESETS.map(preset => (
                    <button
                      key={preset.label}
                      onClick={() => setSettings(s => ({
                        ...s,
                        multiAngle: {
                          ...s.multiAngle,
                          horizontalAngle: preset.horizontalAngle,
                          verticalAngle: preset.verticalAngle,
                          zoom: preset.zoom,
                        }
                      }))}
                      className={cn(
                        "py-2 px-1.5 text-micro rounded-lg border transition-all text-center",
                        settings.multiAngle.horizontalAngle === preset.horizontalAngle &&
                        settings.multiAngle.verticalAngle === preset.verticalAngle
                          ? "bg-primary/20 border-primary text-primary"
                          : "border-border text-secondary hover:bg-primary/5 hover:border-secondary"
                      )}
                      title={`H: ${preset.horizontalAngle}°, V: ${preset.verticalAngle}°`}
                    >
                      {preset.label}
                    </button>
                  ))}
                </div>
              </div>

              <hr className="border-border/30" />

              {/* Horizontal Angle (Camera Rotation) */}
              <AngleSlider
                label="Camera Rotation"
                value={settings.multiAngle.horizontalAngle}
                onChange={(v) => setSettings(s => ({
                  ...s,
                  multiAngle: { ...s.multiAngle, horizontalAngle: v }
                }))}
                min={-180}
                max={180}
                step={1}
                valueDisplay={formatHorizontalAngle(settings.multiAngle.horizontalAngle)}
                showCenterMark
                centerValue={0}
                minLabel="Left"
                maxLabel="Right"
              />

              {/* Vertical Angle (Camera Elevation) */}
              <AngleSlider
                label="Camera Elevation"
                value={settings.multiAngle.verticalAngle}
                onChange={(v) => setSettings(s => ({
                  ...s,
                  multiAngle: { ...s.multiAngle, verticalAngle: v }
                }))}
                min={-30}
                max={90}
                step={1}
                valueDisplay={formatVerticalAngle(settings.multiAngle.verticalAngle)}
                showCenterMark
                centerValue={0}
                minLabel="Low"
                maxLabel="Top"
              />

              {/* Zoom (Camera Distance) */}
              <AngleSlider
                label="Camera Distance"
                value={settings.multiAngle.zoom}
                onChange={(v) => setSettings(s => ({
                  ...s,
                  multiAngle: { ...s.multiAngle, zoom: v }
                }))}
                min={0}
                max={10}
                step={0.5}
                valueDisplay={formatZoom(settings.multiAngle.zoom)}
                showCenterMark
                centerValue={5}
                minLabel="Wide"
                maxLabel="Close"
              />
            </div>
          )}

          {/* Upscale Settings */}
          {mode === 'upscale' && (
            <div className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-micro font-bold text-accent uppercase tracking-wider">Upscale Model</label>
                <div className="space-y-1">
                  {TOPAZ_MODELS_LIST.map(m => (
                    <button
                      key={m.value}
                      onClick={() => setSettings(s => ({ ...s, upscale: { ...s.upscale, model: m.value as any } }))}
                      className={cn(
                        "w-full text-left py-2 px-3 text-xs rounded-lg transition-all",
                         settings.upscale.model === m.value
                          ? "bg-primary/20 text-primary"
                          : "hover:bg-primary/5 text-text"
                      )}
                    >
                      {m.label}
                    </button>
                  ))}
                </div>
              </div>

              <div className="space-y-1.5">
                 <label className="text-micro font-bold text-accent uppercase tracking-wider">
                   Factor ({settings.upscale.upscaleFactor}x)
                 </label>
                 <div className="flex gap-2">
                   {[2, 4].map(fac => (
                      <button
                        key={fac}
                        onClick={() => setSettings(s => ({ ...s, upscale: { ...s.upscale, upscaleFactor: fac } }))}
                        className={cn(
                          "flex-1 py-2 text-xs rounded-lg border transition-all",
                          settings.upscale.upscaleFactor === fac
                            ? "bg-primary/20 border-primary text-primary"
                            : "bg-transparent border-border text-secondary"
                        )}
                      >
                        {fac}x
                      </button>
                   ))}
                 </div>
              </div>
            </div>
          )}

        </div>
      </div>

      {/* Footer / CTA */}
      <div className="p-4 bg-card border-t border-border">

        {/* Active Jobs Mini-View */}
        {activeJobs.length > 0 && (
          <div className="mb-3 space-y-2">
            {activeJobs.map((job) => (
              <div key={job.id} className="p-3 bg-card/50 rounded-lg border border-border/50 space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Loader2 className="h-3 w-3 animate-spin text-primary" />
                    <span className="text-micro font-medium text-text uppercase tracking-wide">
                      {job.type === 'Generate' ? 'Generation' :
                       job.type === 'Upscale' ? 'Upscale' :
                       job.type === 'Refine' ? 'Refine' :
                       job.type === 'MultiAngle' ? 'Multi-Angle' : 'Processing'}
                    </span>
                  </div>
                  <span className="text-micro text-primary font-medium tabular-nums">
                    {job.progress ?? 0}%
                  </span>
                </div>

                {/* Progress Bar */}
                <div className="relative h-1.5 bg-primary/10 rounded-full overflow-hidden">
                  <div
                    className="absolute inset-y-0 left-0 bg-gradient-to-r from-primary to-primary/70 rounded-full transition-all duration-500 ease-out"
                    style={{ width: `${job.progress ?? 0}%` }}
                  />
                </div>

                {/* Progress Message */}
                {job.progressMessage && (
                  <p className="text-micro text-secondary truncate">
                    {job.progressMessage}
                  </p>
                )}
              </div>
            ))}
          </div>
        )}

        <Button
          className="w-full bg-primary hover:bg-primary/90 text-background font-medium h-11 rounded-xl shadow-lg"
          onClick={handleSubmit}
          disabled={isProcessing || !hasSource}
        >
          {isProcessing ? (
             <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
             <Sparkles className="h-4 w-4 mr-2" />
          )}
          {mode === 'generate' ? 'Generate' : mode === 'refine' ? 'Refine' : mode === 'upscale' ? 'Upscale' : 'Move Camera'}
        </Button>
        {maskState && maskState.layers.length > 0 && (
          <p className="text-micro text-secondary mt-1.5 text-center">
            {maskState.layers.filter(l => l.imageData && l.instruction.trim()).length}/{maskState.layers.length} masks ready
          </p>
        )}
      </div>
    </div>
  );
}
