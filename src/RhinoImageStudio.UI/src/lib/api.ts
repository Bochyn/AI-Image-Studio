import { Project, Capture, Generation, CreateProjectRequest, UpdateProjectRequest, GenerateRequest, ReferenceImage, GenerationDebugInfo, MaskLayerPayload, Job } from './types';

const API_BASE = '/api';
const LOCAL_TOKEN_HEADER = 'X-Rhino-Bridge-Token';

declare global {
  interface Window {
    __RHINO_LOCAL_TOKEN?: string;
  }
}

let cachedLocalToken: string | null =
  typeof window !== 'undefined' ? window.__RHINO_LOCAL_TOKEN ?? null : null;

async function ensureLocalToken(): Promise<string | null> {
  if (cachedLocalToken) return cachedLocalToken;

  try {
    const res = await fetch(`${API_BASE}/bootstrap`);
    if (res.ok) {
      const data = await res.json() as { token: string };
      cachedLocalToken = data.token;
    }
  } catch {
    // Backend may be unavailable during isolated UI dev
  }

  return cachedLocalToken;
}

async function apiFetch(input: string, init: RequestInit = {}): Promise<Response> {
  const token = await ensureLocalToken();
  const headers = new Headers(init.headers);
  if (token) headers.set(LOCAL_TOKEN_HEADER, token);
  return fetch(input, { ...init, headers });
}

async function apiError(res: Response, fallback: string): Promise<never> {
  try {
    const body = await res.json() as { error?: string; message?: string; details?: string };
    const message = body.error || body.message || body.details;
    throw new Error(message || fallback);
  } catch {
    try {
      const text = await res.text();
      throw new Error(text || fallback);
    } catch {
      throw new Error(fallback);
    }
  }
}

export interface ConfigInfo {
  hasFalApiKey: boolean;
  hasGeminiApiKey: boolean;
  dataPath: string;
  backendPort: number;
  defaultProvider: 'gemini' | 'fal';
}

export const api = {
  config: {
    get: async (): Promise<ConfigInfo> => {
      const res = await fetch(`${API_BASE}/config`);
      if (!res.ok) return apiError(res, 'Failed to fetch config');
      return res.json();
    },
    setGeminiApiKey: async (apiKey: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/config/gemini-api-key`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey }),
      });
      if (!res.ok) return apiError(res, 'Failed to set Gemini API key');
    },
    setFalApiKey: async (apiKey: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/config/fal-api-key`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey }),
      });
      if (!res.ok) return apiError(res, 'Failed to set fal.ai API key');
    },
  },
  projects: {
    list: async (): Promise<Project[]> => {
      const res = await fetch(`${API_BASE}/projects`);
      if (!res.ok) return apiError(res, 'Failed to fetch projects');
      const data = await res.json();
      // API returns { projects: [], totalCount: number }
      return (data.projects || []).map((project: Project) => ({
        ...project,
        previewUrl: project.previewUrl || project.lastThumbnailUrl || undefined,
      }));
    },
    get: async (id: string): Promise<Project> => {
      const res = await fetch(`${API_BASE}/projects/${id}`);
      if (!res.ok) return apiError(res, 'Failed to fetch project');
      const project = await res.json() as Project;
      return {
        ...project,
        previewUrl: project.previewUrl || project.lastThumbnailUrl || undefined,
      };
    },
    create: async (data: CreateProjectRequest): Promise<Project> => {
      const res = await apiFetch(`${API_BASE}/projects`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) return apiError(res, 'Failed to create project');
      const project = await res.json() as Project;
      return {
        ...project,
        previewUrl: project.previewUrl || project.lastThumbnailUrl || undefined,
      };
    },
    delete: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/projects/${id}`, {
        method: 'DELETE',
      });
      if (!res.ok) return apiError(res, 'Failed to delete project');
    },
    togglePin: async (id: string, pinned: boolean): Promise<Project> => {
      const res = await apiFetch(`${API_BASE}/projects/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isPinned: pinned }),
      });
      if (!res.ok) return apiError(res, 'Failed to update pin status');
      const project = await res.json() as Project;
      return {
        ...project,
        previewUrl: project.previewUrl || project.lastThumbnailUrl || undefined,
      };
    },
    update: async (id: string, data: UpdateProjectRequest): Promise<Project> => {
      const res = await apiFetch(`${API_BASE}/projects/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) return apiError(res, 'Failed to update project');
      const project = await res.json() as Project;
      return {
        ...project,
        previewUrl: project.previewUrl || project.lastThumbnailUrl || undefined,
      };
    }
  },
  captures: {
    list: async (projectId: string): Promise<Capture[]> => {
      const res = await fetch(`${API_BASE}/projects/${projectId}/captures`);
      if (!res.ok) return apiError(res, 'Failed to fetch captures');
      return res.json();
    },
    delete: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/captures/${id}`, {
        method: 'DELETE',
      });
      if (!res.ok) return apiError(res, 'Failed to delete capture');
    },
  },
  jobs: {
    list: async (projectId: string): Promise<Job[]> => {
      const res = await fetch(`${API_BASE}/projects/${projectId}/jobs`);
      if (!res.ok) return apiError(res, 'Failed to fetch jobs');
      return res.json();
    },
  },
  references: {
    list: async (projectId: string): Promise<ReferenceImage[]> => {
      const res = await fetch(`${API_BASE}/projects/${projectId}/references`);
      if (!res.ok) return apiError(res, 'Failed to fetch references');
      return res.json();
    },
    upload: async (projectId: string, file: File): Promise<ReferenceImage> => {
      const formData = new FormData();
      formData.append('image', file);
      const res = await apiFetch(`${API_BASE}/projects/${projectId}/references`, {
        method: 'POST',
        body: formData,
      });
      if (!res.ok) return apiError(res, 'Failed to upload reference image');
      return res.json();
    },
    delete: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/references/${id}`, { method: 'DELETE' });
      if (!res.ok) return apiError(res, 'Failed to delete reference');
    },
  },
  generations: {
    list: async (projectId: string): Promise<Generation[]> => {
      const res = await fetch(`${API_BASE}/projects/${projectId}/generations`);
      if (!res.ok) return apiError(res, 'Failed to fetch generations');
      return res.json();
    },
    listAll: async (): Promise<Generation[]> => {
      const res = await fetch(`${API_BASE}/generations`);
      if (!res.ok) return apiError(res, 'Failed to fetch all generations');
      const data = await res.json();
      // API returns { generations: [], totalCount: number }
      return data.generations || [];
    },
    listGlobal: async (limit = 50, offset = 0): Promise<{ generations: Generation[]; total: number }> => {
      const params = new URLSearchParams({
        limit: String(limit),
        offset: String(offset),
      });
      const res = await fetch(`${API_BASE}/generations?${params.toString()}`);
      if (!res.ok) return apiError(res, 'Failed to fetch global generations');
      const data = await res.json() as { generations?: Generation[]; total?: number };
      return {
        generations: data.generations || [],
        total: data.total || 0,
      };
    },
    create: async (data: GenerateRequest): Promise<Generation> => {
      const res = await apiFetch(`${API_BASE}/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) return apiError(res, 'Failed to start generation');
      return res.json();
    },
    archive: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/generations/${id}`, {
        method: 'DELETE',
      });
      if (!res.ok) return apiError(res, 'Failed to archive generation');
    },
    restore: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/generations/${id}/restore`, {
        method: 'PUT',
      });
      if (!res.ok) return apiError(res, 'Failed to restore generation');
    },
    permanentDelete: async (id: string): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/generations/${id}/permanent`, {
        method: 'DELETE',
      });
      if (!res.ok) return apiError(res, 'Failed to permanently delete generation');
    },
    listArchived: async (projectId: string): Promise<Generation[]> => {
      const res = await fetch(`${API_BASE}/projects/${projectId}/generations/archived`);
      if (!res.ok) return apiError(res, 'Failed to fetch archived generations');
      return res.json();
    },
    getDebugInfo: async (id: string): Promise<GenerationDebugInfo> => {
      const res = await fetch(`${API_BASE}/generations/${id}/debug`);
      if (!res.ok) return apiError(res, 'Debug info not available');
      return res.json();
    },
    getMasks: async (id: string): Promise<MaskLayerPayload[]> => {
      const res = await fetch(`${API_BASE}/generations/${id}/masks`);
      if (!res.ok) return [];
      return res.json();
    },
  },
  multiAngle: {
    create: async (data: MultiAngleRequest): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/multi-angle`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) return apiError(res, 'Failed to start multi-angle generation');
    }
  },
  upscale: {
    create: async (data: UpscaleRequest): Promise<void> => {
      const res = await apiFetch(`${API_BASE}/upscale`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) return apiError(res, 'Failed to start upscale');
    }
  }
};

// Request types for new endpoints
export interface MultiAngleRequest {
  projectId: string;
  sourceGenerationId?: string;  // Either generation or capture
  sourceCaptureId?: string;
  horizontalAngle?: number;     // 0-360 (API format)
  verticalAngle?: number;       // -30 to 90
  zoom?: number;                // 0-10
  loraScale?: number;           // 0-1
  numImages?: number;
}

export interface UpscaleRequest {
  projectId: string;
  sourceGenerationId: string;
  model?: string;
  upscaleFactor?: number;
  faceEnhancement?: boolean;
  outputFormat?: 'jpeg' | 'png';
}
