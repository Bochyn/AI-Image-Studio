/**
 * Model configurations for Rhino Image Studio
 * Each model has its own set of parameters
 */

// ============================================================================
// SHARED TYPES FOR MODEL OPTIONS
// ============================================================================

export interface AspectRatioOption {
  value: string;      // Value sent to API (e.g., "1:1", "square_hd")
  label: string;      // Display label for user
  ratio?: number;     // Numeric ratio (w/h) for pixel calculation, optional
}

export interface ResolutionOption {
  value: string;      // Value sent to API (e.g., "1K", "2K")
  label: string;      // Display label
  pixels: number;     // Base dimension in pixels
}

// ============================================================================
// MODEL-SPECIFIC OPTIONS
// ============================================================================

// Gemini API supported aspect ratios (per official documentation)
const GEMINI_ASPECT_RATIOS: AspectRatioOption[] = [
  { value: '1:1', label: '1:1', ratio: 1 },
  { value: '2:3', label: '2:3', ratio: 2/3 },
  { value: '3:2', label: '3:2', ratio: 3/2 },
  { value: '3:4', label: '3:4', ratio: 3/4 },
  { value: '4:3', label: '4:3', ratio: 4/3 },
  { value: '4:5', label: '4:5', ratio: 4/5 },
  { value: '5:4', label: '5:4', ratio: 5/4 },
  { value: '9:16', label: '9:16', ratio: 9/16 },
  { value: '16:9', label: '16:9', ratio: 16/9 },
  { value: '21:9', label: '21:9', ratio: 21/9 },
];

// Gemini 3.1 Flash supports extended aspect ratios
const GEMINI_3_1_FLASH_ASPECT_RATIOS: AspectRatioOption[] = [
  { value: '1:1', label: '1:1', ratio: 1 },
  { value: '1:4', label: '1:4', ratio: 1/4 },
  { value: '1:8', label: '1:8', ratio: 1/8 },
  { value: '2:3', label: '2:3', ratio: 2/3 },
  { value: '3:2', label: '3:2', ratio: 3/2 },
  { value: '3:4', label: '3:4', ratio: 3/4 },
  { value: '4:1', label: '4:1', ratio: 4/1 },
  { value: '4:3', label: '4:3', ratio: 4/3 },
  { value: '4:5', label: '4:5', ratio: 4/5 },
  { value: '5:4', label: '5:4', ratio: 5/4 },
  { value: '8:1', label: '8:1', ratio: 8/1 },
  { value: '9:16', label: '9:16', ratio: 9/16 },
  { value: '16:9', label: '16:9', ratio: 16/9 },
  { value: '21:9', label: '21:9', ratio: 21/9 },
];

// Gemini 3 Pro supported resolutions
const GEMINI_PRO_RESOLUTIONS: ResolutionOption[] = [
  { value: '1K', label: '1K', pixels: 1024 },
  { value: '2K', label: '2K', pixels: 2048 },
  { value: '4K', label: '4K', pixels: 4096 },
];

// Gemini 3.1 Flash supports multiple resolutions (0.5K uses "512" API value)
const GEMINI_3_1_FLASH_RESOLUTIONS: ResolutionOption[] = [
  { value: '512', label: '0.5K', pixels: 512 },
  { value: '1K', label: '1K', pixels: 1024 },
  { value: '2K', label: '2K', pixels: 2048 },
  { value: '4K', label: '4K', pixels: 4096 },
];

// Seedream v5 Lite Edit image size presets
const SEEDREAM_IMAGE_SIZES: AspectRatioOption[] = [
  { value: 'auto_2K', label: 'Auto 2K' },
  { value: 'auto_3K', label: 'Auto 3K' },
  { value: 'square_hd', label: '1:1 HD', ratio: 1 },
  { value: 'square', label: '1:1', ratio: 1 },
  { value: 'portrait_4_3', label: '3:4', ratio: 3/4 },
  { value: 'portrait_16_9', label: '9:16', ratio: 9/16 },
  { value: 'landscape_4_3', label: '4:3', ratio: 4/3 },
  { value: 'landscape_16_9', label: '16:9', ratio: 16/9 },
];

// GPT-Image 1.5 Edit sizes (pixel-based)
const GPT_IMAGE_SIZES: AspectRatioOption[] = [
  { value: 'auto', label: 'Auto' },
  { value: '1024x1024', label: '1:1', ratio: 1 },
  { value: '1536x1024', label: '3:2', ratio: 3/2 },
  { value: '1024x1536', label: '2:3', ratio: 2/3 },
];

// GPT Image 2 Edit size presets via fal.ai
const GPT_IMAGE_2_SIZES: AspectRatioOption[] = [
  { value: 'auto', label: 'Auto' },
  { value: 'square_hd', label: '1:1 HD', ratio: 1 },
  { value: 'square', label: '1:1', ratio: 1 },
  { value: 'portrait_4_3', label: '3:4', ratio: 3/4 },
  { value: 'portrait_16_9', label: '9:16', ratio: 9/16 },
  { value: 'landscape_4_3', label: '4:3', ratio: 4/3 },
  { value: 'landscape_16_9', label: '16:9', ratio: 16/9 },
];

// ============================================================================
// SHARED CONSTANTS (legacy, kept for compatibility)
// ============================================================================

/** @deprecated Use model-specific aspectRatios instead */
export const ASPECT_RATIOS = [
  { value: 'auto', label: 'Auto' },
  { value: '21:9', label: 'Ultra Wide (21:9)' },
  { value: '16:9', label: 'Widescreen (16:9)' },
  { value: '3:2', label: 'Classic (3:2)' },
  { value: '4:3', label: 'Standard (4:3)' },
  { value: '5:4', label: 'Photo (5:4)' },
  { value: '1:1', label: 'Square (1:1)' },
  { value: '4:5', label: 'Portrait Photo (4:5)' },
  { value: '3:4', label: 'Portrait (3:4)' },
  { value: '2:3', label: 'Tall (2:3)' },
  { value: '9:16', label: 'Vertical (9:16)' },
] as const;

export const OUTPUT_FORMATS = [
  { value: 'jpeg', label: 'JPEG' },
  { value: 'png', label: 'PNG' },
] as const;

// ============================================================================
// MODEL DEFINITIONS
// ============================================================================

export type ModelProvider = 'fal' | 'gemini';

export type ModeType = 'generate' | 'refine' | 'multiangle' | 'upscale';

export interface ModelCapabilities {
  supportsNegativePrompt: boolean;
  supportsSeed: boolean;
  supportsAspectRatio: boolean;
  supportsNumImages: boolean;
  supportsStrength: boolean; // For image-to-image/refine
  supportsReferences: boolean;  // Whether model supports reference images
  supportsMasks: boolean;       // Whether model supports inpainting masks
}

export interface ModelInfo {
  id: string;
  provider: ModelProvider;
  name: string;
  shortName: string;
  description: string;
  capabilities: ModelCapabilities;
  // Model-specific options
  aspectRatios?: AspectRatioOption[];
  resolutions?: ResolutionOption[];
  maxReferences?: number;    // Max reference images (undefined = 0)
  maxMaskLayers?: number;    // Max inpainting mask layers (undefined = 0)
  maxTotalImages?: number;   // Max total images per request: 1 (source) + refs + masks
  qualityOptions?: { value: string; label: string }[];
  fidelityOptions?: { value: string; label: string }[];
}

export const MODELS: Record<string, ModelInfo> = {
  'gemini-3.1-flash-image-preview': {
    id: 'gemini-3.1-flash-image-preview',
    provider: 'gemini',
    name: 'Gemini 3.1 Flash',
    shortName: 'gemini-flash',
    description: 'Fast image generation with extended AR and resolution support',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: false,
      supportsAspectRatio: true,
      supportsNumImages: true,
      supportsStrength: true,
      supportsReferences: true,
      supportsMasks: true,
    },
    aspectRatios: GEMINI_3_1_FLASH_ASPECT_RATIOS,
    resolutions: GEMINI_3_1_FLASH_RESOLUTIONS,
    maxReferences: 14,
    maxMaskLayers: 2,
    maxTotalImages: 16,
  },
  'gemini-3-pro-image-preview': {
    id: 'gemini-3-pro-image-preview',
    provider: 'gemini',
    name: 'Gemini 3 Pro (Preview)',
    shortName: 'gemini-3-pro',
    description: 'High quality, supports 2K/4K resolution',
    capabilities: {
      supportsNegativePrompt: true,
      supportsSeed: false,
      supportsAspectRatio: true,
      supportsNumImages: true,
      supportsStrength: true,
      supportsReferences: true,
      supportsMasks: true,
    },
    aspectRatios: GEMINI_ASPECT_RATIOS,
    resolutions: GEMINI_PRO_RESOLUTIONS,
    maxReferences: 11,
    maxMaskLayers: 8,
    maxTotalImages: 14,
  },
  'fal-ai/bytedance/seedream/v5/lite/edit': {
    id: 'fal-ai/bytedance/seedream/v5/lite/edit',
    provider: 'fal',
    name: 'Seedream v5 Lite',
    shortName: 'seedream',
    description: 'ByteDance high-quality image editing (up to 3K)',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: false,
      supportsAspectRatio: true,
      supportsNumImages: true,
      supportsStrength: false,
      supportsReferences: true,
      supportsMasks: false,
    },
    aspectRatios: SEEDREAM_IMAGE_SIZES,
    maxReferences: 9,
  },
  'fal-ai/gpt-image-1.5/edit': {
    id: 'fal-ai/gpt-image-1.5/edit',
    provider: 'fal',
    name: 'GPT-Image 1.5',
    shortName: 'gpt-image',
    description: 'OpenAI image editing with quality control',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: false,
      supportsAspectRatio: true,
      supportsNumImages: true,
      supportsStrength: false,
      supportsReferences: true,
      supportsMasks: false,
    },
    aspectRatios: GPT_IMAGE_SIZES,
    qualityOptions: [
      { value: 'low', label: 'Low' },
      { value: 'medium', label: 'Medium' },
      { value: 'high', label: 'High' },
    ],
    fidelityOptions: [
      { value: 'low', label: 'Low' },
      { value: 'high', label: 'High' },
    ],
    maxReferences: 4,
  },
  'openai/gpt-image-2/edit': {
    id: 'openai/gpt-image-2/edit',
    provider: 'fal',
    name: 'GPT Image 2',
    shortName: 'gpt-image-2',
    description: 'OpenAI latest image editing model via fal.ai',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: false,
      supportsAspectRatio: true,
      supportsNumImages: true,
      supportsStrength: false,
      supportsReferences: true,
      supportsMasks: false,
    },
    aspectRatios: GPT_IMAGE_2_SIZES,
    qualityOptions: [
      { value: 'low', label: 'Low' },
      { value: 'medium', label: 'Medium' },
      { value: 'high', label: 'High' },
    ],
    maxReferences: 4,
  },
  'fal-ai/qwen-image-edit-2511-multiple-angles': {
    id: 'fal-ai/qwen-image-edit-2511-multiple-angles',
    provider: 'fal',
    name: 'Qwen Multi-Angle',
    shortName: 'qwen-multi-angle',
    description: 'Generate different camera angles',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: true,
      supportsAspectRatio: false,
      supportsNumImages: false,
      supportsStrength: false,
      supportsReferences: false,
      supportsMasks: false,
    },
  },
  'fal-ai/topaz/upscale/image': {
    id: 'fal-ai/topaz/upscale/image',
    provider: 'fal',
    name: 'Topaz Upscale',
    shortName: 'topaz',
    description: 'AI-powered image upscaling',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: false,
      supportsAspectRatio: false,
      supportsNumImages: false,
      supportsStrength: false,
      supportsReferences: false,
      supportsMasks: false,
    },
  },
};

// ============================================================================
// MODE MAPPINGS
// ============================================================================

// Default models for each mode (Flash is default - cheap for testing)
export const MODE_DEFAULTS: Record<ModeType, string> = {
  generate: 'gemini-3.1-flash-image-preview',
  refine: 'gemini-3.1-flash-image-preview',
  multiangle: 'fal-ai/qwen-image-edit-2511-multiple-angles',
  upscale: 'fal-ai/topaz/upscale/image',
};

export const AVAILABLE_MODELS: Record<ModeType, string[]> = {
  generate: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview', 'openai/gpt-image-2/edit', 'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
  refine: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview', 'openai/gpt-image-2/edit', 'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
  multiangle: ['fal-ai/qwen-image-edit-2511-multiple-angles'],
  upscale: ['fal-ai/topaz/upscale/image'],
};

// ============================================================================
// SETTINGS SCHEMAS & TYPES
// ============================================================================

// GEMINI / Generation Settings
export interface GenerationSettings {
  aspectRatio: string;
  resolution: string;
  numImages: number;
  outputFormat: 'jpeg' | 'png';
  seed?: number;
  negativePrompt?: string;
  strength?: number; // 0-1, used for input image influence
  quality?: string;
  inputFidelity?: string;
}

export const DEFAULT_GENERATION_SETTINGS: GenerationSettings = {
  aspectRatio: '1:1',
  resolution: '1K',
  numImages: 1,
  outputFormat: 'jpeg',
  strength: 0.75,
};

// QWEN MULTI-ANGLE
export interface QwenMultiAngleSettings {
  horizontalAngle: number;
  verticalAngle: number;
  zoom: number;
  loraScale: number;
}

export const DEFAULT_QWEN_SETTINGS: QwenMultiAngleSettings = {
  horizontalAngle: 0,
  verticalAngle: 0,
  zoom: 5,
  loraScale: 0.8,
};

// TOPAZ UPSCALE
export type TopazModelType =
  | 'Standard V2'
  | 'High Fidelity V2'
  | 'Graphics'
  | 'Low Resolution V2'
  | 'CG';

export interface TopazUpscaleSettings {
  model: TopazModelType;
  upscaleFactor: number;
  faceEnhancement: boolean;
  outputFormat: 'jpeg' | 'png';
}

export const DEFAULT_TOPAZ_SETTINGS: TopazUpscaleSettings = {
  model: 'Standard V2',
  upscaleFactor: 2,
  faceEnhancement: false,
  outputFormat: 'jpeg',
};

export const TOPAZ_MODELS_LIST = [
  { value: 'Standard V2', label: 'Standard V2' },
  { value: 'High Fidelity V2', label: 'High Fidelity V2' },
  { value: 'Graphics', label: 'Graphics' },
  { value: 'Low Resolution V2', label: 'Low Resolution V2' },
  { value: 'CG', label: 'CG' },
];

// UI-friendly presets: horizontalAngle uses -180..+180 range
export const MULTI_ANGLE_PRESETS = [
  { label: 'Front', horizontalAngle: 0, verticalAngle: 0, zoom: 5 },
  { label: 'Right', horizontalAngle: 90, verticalAngle: 0, zoom: 5 },
  { label: 'Back', horizontalAngle: 180, verticalAngle: 0, zoom: 5 },
  { label: 'Left', horizontalAngle: -90, verticalAngle: 0, zoom: 5 },
  { label: '3/4 Right', horizontalAngle: 45, verticalAngle: 20, zoom: 5 },
  { label: '3/4 Left', horizontalAngle: -45, verticalAngle: 20, zoom: 5 },
  { label: 'Top Down', horizontalAngle: 0, verticalAngle: 90, zoom: 5 },
  { label: 'Low Angle', horizontalAngle: 0, verticalAngle: -30, zoom: 5 },
] as const;

export type MultiAnglePreset = typeof MULTI_ANGLE_PRESETS[number];

// Unified Settings Object for State
export interface AllModelSettings {
  generation: GenerationSettings;
  multiAngle: QwenMultiAngleSettings;
  upscale: TopazUpscaleSettings;
}

export const DEFAULT_ALL_SETTINGS: AllModelSettings = {
  generation: DEFAULT_GENERATION_SETTINGS,
  multiAngle: DEFAULT_QWEN_SETTINGS,
  upscale: DEFAULT_TOPAZ_SETTINGS,
};

// ============================================================================
// HELPERS
// ============================================================================

export function getModelInfo(id: string): ModelInfo | undefined {
  return MODELS[id];
}

/**
 * With 2-image overlay format, masks consume 2 image slots (source + overlay).
 * Returns maxMaskLayers if budget allows, otherwise 0.
 */
export function getAvailableMaskSlots(modelId: string, refCount: number): number {
  const model = MODELS[modelId];
  if (!model?.capabilities.supportsMasks || !model.maxMaskLayers) return 0;
  // 2-image overlay: source(1) + overlay(1) + refs <= maxTotalImages
  if (model.maxTotalImages && (2 + refCount) > model.maxTotalImages) return 0;
  return model.maxMaskLayers;
}

/**
 * Calculate pixel dimensions from aspect ratio and resolution for a specific model
 */
export function calculateDimensions(
  aspectRatio: string,
  resolution: string,
  modelId: string
): { width: number; height: number } {
  const model = MODELS[modelId];

  // Find resolution pixels from model config
  const resOption = model?.resolutions?.find(r => r.value === resolution);
  const baseDimension = resOption?.pixels || 1024;

  // Find aspect ratio from model config
  const arOption = model?.aspectRatios?.find(a => a.value === aspectRatio);

  // If we have a numeric ratio from model config, use it
  if (arOption?.ratio) {
    const ratio = arOption.ratio;
    if (ratio >= 1) {
      return {
        width: baseDimension,
        height: Math.round(baseDimension / ratio),
      };
    } else {
      return {
        width: Math.round(baseDimension * ratio),
        height: baseDimension,
      };
    }
  }

  // Fallback - parse string "W:H"
  const [w, h] = aspectRatio.split(':').map(Number);
  if (w && h) {
    const ratio = w / h;
    if (ratio >= 1) {
      return { width: baseDimension, height: Math.round(baseDimension / ratio) };
    } else {
      return { width: Math.round(baseDimension * ratio), height: baseDimension };
    }
  }

  return { width: baseDimension, height: baseDimension };
}

/** @deprecated Use calculateDimensions instead */
export function getResolutionFromAspectRatio(aspectRatio: string, baseDimension = 1024): { width: number; height: number } {
  if (aspectRatio === 'auto') {
    return { width: baseDimension, height: baseDimension };
  }

  const [w, h] = aspectRatio.split(':').map(Number);
  if (!w || !h) {
    return { width: baseDimension, height: baseDimension };
  }

  const ratio = w / h;
  if (ratio >= 1) {
    return {
      width: baseDimension,
      height: Math.round(baseDimension / ratio),
    };
  } else {
    return {
      width: Math.round(baseDimension * ratio),
      height: baseDimension,
    };
  }
}

// ============================================================================
// QWEN MULTI-ANGLE HELPERS
// ============================================================================

/**
 * Convert UI horizontal angle (-180 to +180) to API angle (0-360)
 * UI: -90=left, 0=front, +90=right, ±180=back
 * API: 0=front, 90=right, 180=back, 270=left
 */
export function horizontalUiToApi(uiAngle: number): number {
  return ((uiAngle % 360) + 360) % 360;
}

/**
 * Convert API horizontal angle (0-360) to UI angle (-180 to +180)
 * Useful for loading presets or saved values
 */
export function horizontalApiToUi(apiAngle: number): number {
  const normalized = ((apiAngle % 360) + 360) % 360;
  if (normalized > 180) return normalized - 360;
  return normalized;
}

/**
 * Format horizontal angle for display
 * Returns human-readable string like "Left 45°" or "Front"
 */
export function formatHorizontalAngle(uiAngle: number): string {
  if (uiAngle === 0) return 'Front';
  if (uiAngle === 90) return 'Right 90°';
  if (uiAngle === -90) return 'Left 90°';
  if (uiAngle === 180 || uiAngle === -180) return 'Back';
  if (uiAngle > 0) return `Right ${uiAngle}°`;
  return `Left ${Math.abs(uiAngle)}°`;
}

/**
 * Format vertical angle for display
 */
export function formatVerticalAngle(angle: number): string {
  if (angle === 0) return 'Eye Level';
  if (angle === 90) return 'Top';
  if (angle === -30) return 'Low';
  if (angle > 0) return `Up ${angle}°`;
  return `Down ${Math.abs(angle)}°`;
}

/**
 * Format zoom for display
 */
export function formatZoom(zoom: number): string {
  if (zoom <= 2) return `Wide (${zoom.toFixed(1)})`;
  if (zoom <= 6) return `Medium (${zoom.toFixed(1)})`;
  return `Close (${zoom.toFixed(1)})`;
}
