import { lazy, Suspense, useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '@/lib/api';
import { Project, Capture, Generation, ReferenceImage, SelectedItem, MASK_COLOR_NAMES, MASK_COLORS, CaptureDisplayMode } from '@/lib/types';
import type { MaskPayload } from '@/lib/types';
import { calculateDimensions, AllModelSettings, DEFAULT_ALL_SETTINGS, MODELS } from '@/lib/models';
import { exportMasksAsOverlay } from '@/lib/maskUtils';
import { useJobs } from '@/hooks/useJobs';
import { useRhino } from '@/hooks/useRhino';
import { useStudioMasks } from '@/hooks/useStudioMasks';
import { AssetsPanel } from '@/components/Studio/AssetsPanel';
import { CanvasStage } from '@/components/Studio/CanvasStage';
import { InspectorPanel } from '@/components/Studio/InspectorPanel';
import { ReferencePanel } from '@/components/Studio/ReferencePanel';
import { Button } from '@/components/Common/Button';
import { ConfirmDialog } from '@/components/Common/ConfirmDialog';
import { useToast } from '@/components/Common/ToastProvider';
import { ThemeSwitch } from '@/components/Common/ThemeSwitch';
import { Settings, Home } from 'lucide-react';

const SettingsModal = lazy(() => import('@/components/Settings/SettingsModal').then((module) => ({ default: module.SettingsModal })));
const DebugModal = lazy(() => import('@/components/Studio/DebugModal').then((module) => ({ default: module.DebugModal })));

export function StudioPage() {
  const { sessionId: projectId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const initialGenId = searchParams.get('gen');
  const { rhino, isAvailable: rhinoAvailable, error: rhinoError, retry: retryRhino } = useRhino();
  const { jobs, subscribe, unsubscribe } = useJobs();
  const { toast } = useToast();

  const [project, setProject] = useState<Project | null>(null);
  const [captures, setCaptures] = useState<Capture[]>([]);
  const [generations, setGenerations] = useState<Generation[]>([]);
  const [archivedGenerations, setArchivedGenerations] = useState<Generation[]>([]);

  // Unified selection state
  const [selectedItem, setSelectedItem] = useState<SelectedItem | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isCapturing, setIsCapturing] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [assetsCollapsed, setAssetsCollapsed] = useState(false);
  const [debugGenerationId, setDebugGenerationId] = useState<string | null>(null);
  const [captureToDeleteId, setCaptureToDeleteId] = useState<string | null>(null);
  const [captureDisplayMode, setCaptureDisplayMode] = useState<CaptureDisplayMode>('viewport');
  const [generationToDeletePermanentlyId, setGenerationToDeletePermanentlyId] = useState<string | null>(null);
  const handledSucceededJobIdRef = useRef<string | null>(null);

  // Editor settings from InspectorPanel (for capture sync)
  const [editorSettings, setEditorSettings] = useState<AllModelSettings>(DEFAULT_ALL_SETTINGS);
  const [currentModelId, setCurrentModelId] = useState<string>('gemini-3-pro-image-preview');
  const [references, setReferences] = useState<ReferenceImage[]>([]);
  const [showReferences, setShowReferences] = useState(false);

  const getErrorMessage = (error: unknown, fallback: string): string => {
    if (error instanceof Error && error.message) return error.message;
    return fallback;
  };

  const currentModelInfo = MODELS[currentModelId];
  const supportsRefs = currentModelInfo?.capabilities.supportsReferences ?? false;
  const maxRefs = currentModelInfo?.maxReferences ?? 0;
  const supportsMasks = currentModelInfo?.capabilities.supportsMasks ?? false;
  const maxMaskLayers = currentModelInfo?.maxMaskLayers ?? 0;
  const {
    maskState,
    isMaskMode,
    sourceImageDimensions,
    setBrushSize,
    handleAddMaskLayer,
    handleRemoveMaskLayer,
    handleSelectMaskLayer,
    handleUpdateMaskInstruction,
    handleToggleMaskVisibility,
    handleMaskLayerUpdate,
    handleToggleMaskMode,
  } = useStudioMasks({
    selectedItem,
    currentModelId,
    referencesCount: references.length,
    maxMaskLayers,
  });

  const refreshGenerations = useCallback(async () => {
    if (!projectId) return;
    const [generationsData, archivedData] = await Promise.all([
      api.generations.list(projectId),
      api.generations.listArchived(projectId),
    ]);
    setGenerations(generationsData);
    setArchivedGenerations(archivedData);
    setSelectedItem((current) => {
      if (current) return current;
      if (generationsData.length > 0) return { type: 'generation', data: generationsData[0] };
      return captures[0] ? { type: 'capture', data: captures[0] } : null;
    });
  }, [projectId, captures]);

  // Load project data
  const loadData = useCallback(async () => {
    if (!projectId) return;
    try {
      const [projectData, capturesData, generationsData, referencesData, archivedData] = await Promise.all([
        api.projects.get(projectId),
        api.captures.list(projectId),
        api.generations.list(projectId),
        api.references.list(projectId),
        api.generations.listArchived(projectId),
      ]);
      setProject(projectData);
      setCaptures(capturesData);
      setGenerations(generationsData);
      setReferences(referencesData);
      setArchivedGenerations(archivedData);

      setSelectedItem((current) => {
        if (current) return current;
        if (initialGenId) {
          const target = generationsData.find(g => g.id === initialGenId);
          if (target) return { type: 'generation', data: target };
        }
        if (generationsData.length > 0) return { type: 'generation', data: generationsData[0] };
        return capturesData[0] ? { type: 'capture', data: capturesData[0] } : null;
      });
    } catch (error) {
      console.error('Failed to load project data:', error);
      toast({
        title: 'Failed to load project data',
        description: getErrorMessage(error, 'Unknown error while loading project'),
        variant: 'error',
      });
    } finally {
      setIsLoading(false);
    }
  }, [projectId, toast]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Subscribe to SSE events
  useEffect(() => {
    if (!projectId) return;
    subscribe(projectId);
    return () => unsubscribe();
  }, [projectId, subscribe, unsubscribe]);

  // Reload generations when job completes
  useEffect(() => {
    const completedJob = jobs.find(j => j.status === 'Succeeded');
    if (!completedJob) return;
    if (handledSucceededJobIdRef.current === completedJob.id) return;

    handledSucceededJobIdRef.current = completedJob.id;
    refreshGenerations().catch((error) => {
      toast({
        title: 'Refresh failed',
        description: getErrorMessage(error, 'Could not refresh generations after job completion'),
        variant: 'error',
      });
    });
  }, [jobs, refreshGenerations, toast]);

  // Hide reference panel when model doesn't support references
  useEffect(() => {
    if (!supportsRefs) {
      setShowReferences(false);
    }
  }, [supportsRefs]);

  // Handle settings changes from InspectorPanel
  const handleSettingsChange = useCallback((settings: AllModelSettings, modelId: string) => {
    setEditorSettings(settings);
    setCurrentModelId(modelId);
  }, []);

  // Capture viewport with AR/Resolution from editor settings
  const handleCapture = async () => {
    if (!projectId || !rhino) return;

    // Get dimensions from current editor settings
    const { aspectRatio, resolution } = editorSettings.generation;
    const { width, height } = calculateDimensions(aspectRatio, resolution, currentModelId);
    const displayMode = captureDisplayMode === 'viewport' ? 'Current' : captureDisplayMode;

    setIsCapturing(true);
    try {
      const captureId = await rhino.CaptureViewport(projectId, width, height, displayMode);
      if (captureId) {
        await loadData();
        const newCaptures = await api.captures.list(projectId);
        setCaptures(newCaptures);
        const newCapture = newCaptures.find(c => c.id === captureId);
        if (newCapture) setSelectedItem({ type: 'capture', data: newCapture });
      }
    } catch (error) {
      console.error('Capture failed:', error);
      toast({
        title: 'Capture failed',
        description: getErrorMessage(error, 'Rhino viewport capture failed'),
        variant: 'error',
      });
    } finally {
      setIsCapturing(false);
    }
  };

  const handleGenerate = async (prompt: string, settings: any) => {
    if (!projectId) return;

    try {
      const mode = settings?.mode || 'generate';

      // Determine source based on selected item type
      let sourceCaptureId: string | undefined;
      let sourceGenerationId: string | undefined;

      if (selectedItem) {
        if (selectedItem.type === 'capture') {
          // It's a Capture
          sourceCaptureId = selectedItem.data.id;
        } else if (selectedItem.type === 'generation') {
          // It's a Generation
          sourceGenerationId = selectedItem.data.id;
        }
      }

      if (mode === 'multiangle') {
        // Multi-angle works with both capture and generation
        if (!sourceGenerationId && !sourceCaptureId) {
          toast({
            title: 'Source required',
            description: 'Multi-angle requires a capture or generation source image.',
            variant: 'error',
          });
          return;
        }
        await api.multiAngle.create({
          projectId,
          sourceGenerationId,
          sourceCaptureId,
          horizontalAngle: settings?.horizontalAngle,
          verticalAngle: settings?.verticalAngle,
          zoom: settings?.zoom,
          loraScale: settings?.loraScale,
        });
      } else if (mode === 'upscale') {
        // Upscale requires a source generation
        if (!sourceGenerationId) {
          toast({
            title: 'Source required',
            description: 'Upscale requires a generated image as source.',
            variant: 'error',
          });
          return;
        }
        await api.upscale.create({
          projectId,
          sourceGenerationId,
          model: settings?.model,
          upscaleFactor: settings?.upscaleFactor,
          faceEnhancement: settings?.faceEnhancement,
          outputFormat: settings?.outputFormat,
        });
      } else {
        // Generate or Refine — export mask overlay if present
        let maskPayload: MaskPayload | undefined;
        if (maskState.layers.length > 0) {
          const validMasks = maskState.layers.filter(l => l.imageData && l.instruction.trim());
          const currentImage = getDisplayImage();
          if (validMasks.length > 0 && currentImage) {
            const overlayImageBase64 = await exportMasksAsOverlay(
              currentImage,
              validMasks.map(l => ({ imageData: l.imageData!, color: l.color }))
            );
            maskPayload = {
              overlayImageBase64,
              layers: validMasks.map((layer, i) => ({
                color: layer.color,
                colorName: MASK_COLOR_NAMES[MASK_COLORS.indexOf(layer.color as any)] || `color${i + 1}`,
                instruction: layer.instruction.trim(),
              })),
            };
          }
        }

        await api.generations.create({
          projectId,
          prompt,
          sourceCaptureId,
          parentGenerationId: sourceGenerationId,
          model: settings?.model,
          aspectRatio: settings?.aspectRatio,
          resolution: settings?.resolution,
          numImages: settings?.numImages ?? 1,
          outputFormat: settings?.outputFormat ?? 'Png',
          quality: settings?.quality,
          inputFidelity: settings?.inputFidelity,
          referenceImageIds: supportsRefs && showReferences && references.length > 0 ? references.map(r => r.id) : undefined,
          maskPayload,
        });
      }
      // Job will be tracked via SSE
    } catch (error) {
      console.error('Generate failed:', error);
      toast({
        title: 'Generation failed',
        description: getErrorMessage(error, 'Request could not be submitted'),
        variant: 'error',
      });
    }
  };

  const handleDelete = async (id: string, type: 'capture' | 'generation') => {
    if (!projectId) return;
    if (type === 'capture') {
       setCaptureToDeleteId(id);
    } else {
      try {
        await api.generations.archive(id);
        const archived = generations.find(g => g.id === id);
        setGenerations(prev => prev.filter(g => g.id !== id));
        if (archived) {
          setArchivedGenerations(prev => [{ ...archived, isArchived: true, archivedAt: new Date().toISOString() }, ...prev]);
        }
        if (selectedItem?.data.id === id) setSelectedItem(null);
      } catch (error) {
        console.error('Failed to archive generation:', error);
        toast({
          title: 'Archive failed',
          description: getErrorMessage(error, 'Could not archive generation'),
          variant: 'error',
        });
        loadData();
      }
    }
  };

  const handleRestore = async (id: string) => {
    if (!projectId) return;
    try {
      await api.generations.restore(id);
      const restored = archivedGenerations.find(g => g.id === id);
      setArchivedGenerations(prev => prev.filter(g => g.id !== id));
      if (restored) {
        setGenerations(prev => [{ ...restored, isArchived: false, archivedAt: undefined }, ...prev]);
      }
    } catch (error) {
      console.error('Failed to restore generation:', error);
      toast({
        title: 'Restore failed',
        description: getErrorMessage(error, 'Could not restore generation'),
        variant: 'error',
      });
      loadData();
    }
  };

  const handlePermanentDelete = async (id: string) => {
    if (!projectId) return;
    setGenerationToDeletePermanentlyId(id);
  };

  const confirmDeleteCapture = async () => {
    if (!captureToDeleteId) return;
    try {
      await api.captures.delete(captureToDeleteId);
      setCaptures(prev => prev.filter(c => c.id !== captureToDeleteId));
      if (selectedItem?.data.id === captureToDeleteId) setSelectedItem(null);
      setCaptureToDeleteId(null);
    } catch (error) {
      toast({
        title: 'Delete failed',
        description: getErrorMessage(error, 'Could not delete capture'),
        variant: 'error',
      });
    }
  };

  const confirmPermanentDeleteGeneration = async () => {
    if (!generationToDeletePermanentlyId) return;
    try {
      await api.generations.permanentDelete(generationToDeletePermanentlyId);
      setArchivedGenerations(prev => prev.filter(g => g.id !== generationToDeletePermanentlyId));
      if (selectedItem?.data.id === generationToDeletePermanentlyId) setSelectedItem(null);
      setGenerationToDeletePermanentlyId(null);
    } catch (error) {
      console.error('Failed to permanently delete generation:', error);
      toast({
        title: 'Delete failed',
        description: getErrorMessage(error, 'Could not delete generation permanently'),
        variant: 'error',
      });
      loadData();
    }
  };

  const handleDebug = useCallback((id: string) => {
    setDebugGenerationId(id);
  }, []);

  const handleDownload = () => {
    if (!selectedItem) return;
    const url = selectedItem.data.imageUrl || '';
    if (url) {
      const link = document.createElement('a');
      link.href = url;
      link.download = `rhino-image-${selectedItem.data.id}.png`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  };

  // Helper to get image URL for Canvas
  const getDisplayImage = (): string | null => {
    if (!selectedItem) return null;
    const url = selectedItem.data.imageUrl || null;
    return url || null;
  };

  // Helper to get original image for A/B comparison
  const getOriginalImage = (): string | null => {
    if (!selectedItem) return null;

    // Captures have no "original" - they ARE the original
    if (selectedItem.type === 'capture') return null;

    // Generation - find source capture or parent generation
    const generation = selectedItem.data;

    // Priority 1: Source capture (direct input)
    if (generation.sourceCaptureId) {
      const sourceCapture = captures.find(c => c.id === generation.sourceCaptureId);
      if (sourceCapture) return sourceCapture.imageUrl;
    }

    // Priority 2: Parent generation (refinement chain)
    if (generation.parentGenerationId) {
      const parentGen = generations.find(g => g.id === generation.parentGenerationId)
        || archivedGenerations.find(g => g.id === generation.parentGenerationId);
      if (parentGen?.imageUrl) return parentGen.imageUrl;
    }

    return null;
  };

  // Find active job for progress display
  const activeJob = jobs.find(j => j.status === 'Running') || null;

  if (isLoading) {
    return (
      <div className="flex h-screen w-full gap-3 bg-background p-3">
        <div className="w-80 shrink-0 rounded-2xl border border-border bg-panel p-4">
          <div className="h-10 animate-pulse rounded-lg bg-card" />
          <div className="mt-4 grid grid-cols-2 gap-3">
            <div className="aspect-square animate-pulse rounded-xl bg-card" />
            <div className="aspect-square animate-pulse rounded-xl bg-card" />
            <div className="aspect-square animate-pulse rounded-xl bg-card" />
            <div className="aspect-square animate-pulse rounded-xl bg-card" />
          </div>
        </div>
        <div className="flex-1 rounded-2xl border border-border bg-panel p-6">
          <div className="h-full animate-pulse rounded-xl bg-card" />
        </div>
        <div className="w-80 shrink-0 rounded-2xl border border-border bg-panel p-4">
          <div className="h-8 w-24 animate-pulse rounded bg-card" />
          <div className="mt-4 space-y-3">
            <div className="h-20 animate-pulse rounded-xl bg-card" />
            <div className="h-12 animate-pulse rounded-xl bg-card" />
            <div className="h-12 animate-pulse rounded-xl bg-card" />
            <div className="h-12 animate-pulse rounded-xl bg-card" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen flex-col bg-background text-text overflow-hidden">
      {/* App Header */}
      <header className="h-14 flex items-center justify-between px-4 border-b border-border/50 bg-background flex-shrink-0 z-10">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/')} className="hover:bg-primary/5">
            <Home className="h-5 w-5 text-secondary" />
          </Button>
          <div className="flex items-center gap-3">
             <div className="h-6 w-px bg-border" />
             <h1 className="font-medium text-sm tracking-wide text-primary">{project?.name || 'Untitled Project'}</h1>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {!rhinoAvailable && (
            <div className="flex items-center gap-2 px-3 py-1 bg-secondary/10 border border-secondary/20 text-secondary text-xs">
              <span>{rhinoError ?? 'Rhino Bridge Disconnected'}</span>
              <Button variant="ghost" size="sm" onClick={() => void retryRhino()} className="h-6 px-2 text-xs">
                Retry
              </Button>
            </div>
          )}
          <ThemeSwitch />
          <Button variant="ghost" size="icon" onClick={() => setShowSettings(true)} className="hover:bg-primary/5">
            <Settings className="h-5 w-5 text-secondary" />
          </Button>
        </div>
      </header>

      {/* Main Workspace Grid */}
      <div className="flex-1 flex gap-3 p-3 overflow-hidden">

        {/* Left: Assets */}
        <div className={`flex-shrink-0 transition-all duration-300 ease-[cubic-bezier(0.25,0.1,0.25,1)] ${assetsCollapsed ? 'w-16' : 'w-80'}`}>
          <AssetsPanel
            captures={captures}
            generations={generations}
            selectedItem={selectedItem}
            onSelect={setSelectedItem}
            onCapture={handleCapture}
            onDelete={handleDelete}
            isCapturing={isCapturing}
            rhinoAvailable={rhinoAvailable}
            isCollapsed={assetsCollapsed}
            onToggleCollapse={() => setAssetsCollapsed(!assetsCollapsed)}
            archivedGenerations={archivedGenerations}
            onRestore={handleRestore}
            onPermanentDelete={handlePermanentDelete}
            onDebug={handleDebug}
            captureDisplayMode={captureDisplayMode}
            onCaptureDisplayModeChange={setCaptureDisplayMode}
          />
        </div>

        {/* Center: Canvas + Reference Panel */}
        <div className="flex-1 min-w-0 flex flex-col gap-2">
          <div className="flex-1 min-h-0">
            <CanvasStage
              currentImage={getDisplayImage()}
              originalImage={getOriginalImage()}
              isProcessing={!!activeJob}
              activeJob={activeJob}
              onDownload={handleDownload}
              supportsReferences={supportsRefs}
              hasReferences={references.length > 0}
              onToggleReferences={() => setShowReferences(!showReferences)}
              captures={captures}
              generations={generations}
              selectedItemId={selectedItem?.data.id || null}
              maskState={maskState}
              onMaskLayerUpdate={handleMaskLayerUpdate}
              isMaskMode={isMaskMode}
              onToggleMaskMode={handleToggleMaskMode}
              supportsMasks={supportsMasks}
              sourceWidth={sourceImageDimensions?.w}
              sourceHeight={sourceImageDimensions?.h}
              brushSize={maskState.brush.size}
              onBrushSizeChange={setBrushSize}
            />
          </div>
          {showReferences && supportsRefs && (
            <ReferencePanel
              projectId={projectId!}
              references={references}
              maxReferences={maxRefs}
              onReferencesChange={setReferences}
              onClose={() => setShowReferences(false)}
            />
          )}
        </div>

        {/* Right: Inspector */}
        <div className="w-80 flex-shrink-0">
          <InspectorPanel
            selectedCapture={selectedItem?.type === 'capture' ? selectedItem.data : null}
            selectedGeneration={selectedItem?.type === 'generation' ? selectedItem.data : null}
            onGenerate={handleGenerate}
            onSettingsChange={handleSettingsChange}
            jobs={jobs}
            maskState={maskState}
            onAddMaskLayer={handleAddMaskLayer}
            onRemoveMaskLayer={handleRemoveMaskLayer}
            onSelectMaskLayer={handleSelectMaskLayer}
            onUpdateMaskInstruction={handleUpdateMaskInstruction}
            onToggleMaskVisibility={handleToggleMaskVisibility}
            isMaskMode={isMaskMode}
            onToggleMaskMode={handleToggleMaskMode}
            maxMaskLayers={maxMaskLayers}
            supportsMasks={supportsMasks}
          />
        </div>

      </div>

      <Suspense fallback={null}>
        <SettingsModal
          isOpen={showSettings}
          onClose={() => setShowSettings(false)}
        />
      </Suspense>

      <Suspense fallback={null}>
        <DebugModal
          isOpen={!!debugGenerationId}
          onClose={() => setDebugGenerationId(null)}
          generationId={debugGenerationId}
        />
      </Suspense>

      <ConfirmDialog
        isOpen={!!captureToDeleteId}
        title="Delete Capture"
        description="Are you sure you want to delete this capture?"
        confirmLabel="Delete"
        cancelLabel="Cancel"
        destructive
        onConfirm={confirmDeleteCapture}
        onCancel={() => setCaptureToDeleteId(null)}
      />

      <ConfirmDialog
        isOpen={!!generationToDeletePermanentlyId}
        title="Permanent Delete"
        description="Permanently delete this generation? This action cannot be undone."
        confirmLabel="Delete Permanently"
        cancelLabel="Cancel"
        destructive
        onConfirm={confirmPermanentDeleteGeneration}
        onCancel={() => setGenerationToDeletePermanentlyId(null)}
      />
    </div>
  );
}
