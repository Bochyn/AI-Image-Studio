import { useCallback, useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { getAvailableMaskSlots } from '@/lib/models';
import { importMaskFromBase64 } from '@/lib/maskUtils';
import { MASK_COLORS, MaskLayer, MaskState, SelectedItem } from '@/lib/types';

interface UseStudioMasksParams {
  selectedItem: SelectedItem | null;
  currentModelId: string;
  referencesCount: number;
  maxMaskLayers: number;
}

export function useStudioMasks({
  selectedItem,
  currentModelId,
  referencesCount,
  maxMaskLayers,
}: UseStudioMasksParams) {
  const [maskState, setMaskState] = useState<MaskState>({
    layers: [],
    activeLayerId: null,
    brush: { size: 20, mode: 'brush' },
  });
  const [isMaskMode, setIsMaskMode] = useState(false);
  const [sourceImageDimensions, setSourceImageDimensions] = useState<{ w: number; h: number } | null>(null);

  const handleAddMaskLayer = useCallback(() => {
    setMaskState((prev) => {
      if (prev.layers.length >= maxMaskLayers) return prev;
      const colorIndex = prev.layers.length % MASK_COLORS.length;
      const newLayer: MaskLayer = {
        id: crypto.randomUUID(),
        name: `Mask ${prev.layers.length + 1}`,
        color: MASK_COLORS[colorIndex],
        instruction: '',
        visible: true,
        imageData: null,
      };
      return {
        ...prev,
        layers: [...prev.layers, newLayer],
        activeLayerId: newLayer.id,
      };
    });
  }, [maxMaskLayers]);

  const handleRemoveMaskLayer = useCallback((layerId: string) => {
    setMaskState((prev) => {
      const filtered = prev.layers.filter((layer) => layer.id !== layerId);
      return {
        ...prev,
        layers: filtered,
        activeLayerId: prev.activeLayerId === layerId ? (filtered[0]?.id ?? null) : prev.activeLayerId,
      };
    });
  }, []);

  const handleSelectMaskLayer = useCallback((layerId: string) => {
    setMaskState((prev) => ({ ...prev, activeLayerId: layerId }));
  }, []);

  const handleUpdateMaskInstruction = useCallback((layerId: string, instruction: string) => {
    setMaskState((prev) => ({
      ...prev,
      layers: prev.layers.map((layer) => (layer.id === layerId ? { ...layer, instruction } : layer)),
    }));
  }, []);

  const handleToggleMaskVisibility = useCallback((layerId: string) => {
    setMaskState((prev) => ({
      ...prev,
      layers: prev.layers.map((layer) => (layer.id === layerId ? { ...layer, visible: !layer.visible } : layer)),
    }));
  }, []);

  const handleMaskLayerUpdate = useCallback((layerId: string, imageData: ImageData) => {
    setMaskState((prev) => ({
      ...prev,
      layers: prev.layers.map((layer) => (layer.id === layerId ? { ...layer, imageData } : layer)),
    }));
  }, []);

  const handleToggleMaskMode = useCallback(() => {
    setIsMaskMode((prev) => !prev);
  }, []);

  const setBrushSize = useCallback((size: number) => {
    setMaskState((prev) => ({
      ...prev,
      brush: { ...prev.brush, size },
    }));
  }, []);

  // Load source image dimensions for current selected image
  useEffect(() => {
    const imageUrl = selectedItem?.data.imageUrl;
    if (!imageUrl) {
      setSourceImageDimensions(null);
      return;
    }

    const img = new Image();
    img.onload = () => setSourceImageDimensions({ w: img.naturalWidth, h: img.naturalHeight });
    img.src = imageUrl;
  }, [selectedItem]);

  // Clear masks when selection changes and restore mask history for generations
  useEffect(() => {
    setMaskState((prev) => ({
      ...prev,
      layers: [],
      activeLayerId: null,
    }));
    setIsMaskMode(false);

    const generation = selectedItem?.type === 'generation' ? selectedItem.data : null;
    if (!generation) return;

    let cancelled = false;
    api.generations.getMasks(generation.id).then(async (masksData: any) => {
      if (cancelled) return;

      if (masksData.overlayImageBase64) {
        if (masksData.layers && masksData.layers.length > 0) {
          const restoredLayers: MaskLayer[] = masksData.layers.map((layer: any, index: number) => ({
            id: crypto.randomUUID(),
            name: `Mask ${index + 1}`,
            color: layer.color || MASK_COLORS[index % MASK_COLORS.length],
            instruction: layer.instruction || '',
            visible: true,
            imageData: null,
          }));
          if (!cancelled) {
            setMaskState((prev) => ({
              ...prev,
              layers: restoredLayers,
              activeLayerId: restoredLayers[0]?.id || null,
            }));
          }
        }
      } else if (Array.isArray(masksData) && masksData.length > 0) {
        const layers: MaskLayer[] = await Promise.all(
          masksData.map(async (payload: any, index: number) => {
            const color = MASK_COLORS[index % MASK_COLORS.length];
            const imageData = await importMaskFromBase64(payload.maskImageBase64, color);
            return {
              id: crypto.randomUUID(),
              name: `Mask ${index + 1}`,
              color,
              instruction: payload.instruction,
              visible: true,
              imageData,
            };
          })
        );

        if (!cancelled) {
          setMaskState((prev) => ({
            ...prev,
            layers,
            activeLayerId: layers[0]?.id ?? null,
          }));
        }
      }
    }).catch(() => {
      // no mask history for selected generation
    });

    return () => {
      cancelled = true;
    };
  }, [selectedItem?.data.id]);

  // Enforce model/reference mask slot budget
  useEffect(() => {
    const available = getAvailableMaskSlots(currentModelId, referencesCount);
    setMaskState((prev) => {
      if (prev.layers.length <= available) return prev;
      const trimmed = prev.layers.slice(0, available);
      return {
        ...prev,
        layers: trimmed,
        activeLayerId: trimmed.some((layer) => layer.id === prev.activeLayerId)
          ? prev.activeLayerId
          : (trimmed[0]?.id ?? null),
      };
    });
  }, [currentModelId, referencesCount]);

  return {
    maskState,
    isMaskMode,
    sourceImageDimensions,
    setMaskState,
    setIsMaskMode,
    setBrushSize,
    handleAddMaskLayer,
    handleRemoveMaskLayer,
    handleSelectMaskLayer,
    handleUpdateMaskInstruction,
    handleToggleMaskVisibility,
    handleMaskLayerUpdate,
    handleToggleMaskMode,
  };
}
