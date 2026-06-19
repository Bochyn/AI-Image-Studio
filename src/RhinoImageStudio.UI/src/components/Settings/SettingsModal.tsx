import { useState, useEffect, useId, useRef } from 'react';
import { api, ConfigInfo } from '@/lib/api';
import { Button } from '@/components/Common/Button';
import { Label } from '@/components/Common/Label';
import { Card } from '@/components/Common/Card';
import { X, Key, CheckCircle, AlertCircle, Loader2, ExternalLink, Sparkles } from 'lucide-react';
import { useDialogA11y } from '@/hooks/useDialogA11y';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function SettingsModal({ isOpen, onClose }: SettingsModalProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  useDialogA11y(isOpen, dialogRef, onClose);

  const [geminiApiKey, setGeminiApiKey] = useState('');
  const [falApiKey, setFalApiKey] = useState('');
  const [config, setConfig] = useState<ConfigInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [savingGemini, setSavingGemini] = useState(false);
  const [savingFal, setSavingFal] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successGemini, setSuccessGemini] = useState(false);
  const [successFal, setSuccessFal] = useState(false);

  useEffect(() => {
    if (isOpen) {
      loadConfig();
    }
  }, [isOpen]);

  const loadConfig = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.config.get();
      setConfig(data);
    } catch (_err) {
      setError('Failed to load configuration');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveGeminiApiKey = async () => {
    if (!geminiApiKey.trim()) {
      setError('Please enter a Gemini API key');
      return;
    }

    setSavingGemini(true);
    setError(null);
    setSuccessGemini(false);

    try {
      await api.config.setGeminiApiKey(geminiApiKey.trim());
      setSuccessGemini(true);
      setGeminiApiKey('');
      await loadConfig();
      setTimeout(() => setSuccessGemini(false), 3000);
    } catch (_err) {
      setError('Failed to save Gemini API key');
    } finally {
      setSavingGemini(false);
    }
  };

  const handleSaveFalApiKey = async () => {
    if (!falApiKey.trim()) {
      setError('Please enter a fal.ai API key');
      return;
    }

    setSavingFal(true);
    setError(null);
    setSuccessFal(false);

    try {
      await api.config.setFalApiKey(falApiKey.trim());
      setSuccessFal(true);
      setFalApiKey('');
      await loadConfig();
      setTimeout(() => setSuccessFal(false), 3000);
    } catch (_err) {
      setError('Failed to save fal.ai API key');
    } finally {
      setSavingFal(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="modal-backdrop absolute inset-0 bg-background/80 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className="modal-panel relative bg-panel border border-border rounded-2xl shadow-xl w-full max-w-lg mx-4"
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border">
          <h2 id={titleId} className="text-lg font-semibold text-primary">Settings</h2>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close settings dialog">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Content */}
        <div className="p-4 space-y-4 max-h-[70vh] overflow-y-auto">
          {loading ? (
            <div className="space-y-4 py-2">
              <div className="rounded-xl border border-border bg-card p-4">
                <div className="h-4 w-40 animate-pulse rounded bg-card-hover" />
                <div className="mt-3 h-3 w-3/4 animate-pulse rounded bg-card-hover" />
                <div className="mt-4 h-9 w-full animate-pulse rounded bg-card-hover" />
                <div className="mt-2 h-9 w-full animate-pulse rounded bg-card-hover" />
              </div>
              <div className="rounded-xl border border-border bg-card p-4">
                <div className="h-4 w-32 animate-pulse rounded bg-card-hover" />
                <div className="mt-3 h-3 w-2/3 animate-pulse rounded bg-card-hover" />
                <div className="mt-4 h-9 w-full animate-pulse rounded bg-card-hover" />
                <div className="mt-2 h-9 w-full animate-pulse rounded bg-card-hover" />
              </div>
            </div>
          ) : (
            <>
              {/* Gemini API Key Section - PRIMARY */}
              <Card className="p-4 bg-card border-primary/30">
                <div className="flex items-center gap-2 mb-3">
                  <Sparkles className="h-4 w-4 text-primary" />
                  <Label className="text-sm font-medium text-primary">Gemini API Key</Label>
                  <span className="text-xs text-primary bg-primary/10 px-2 py-0.5 rounded-full">Primary</span>
                </div>

                <p className="text-xs text-secondary mb-3">
                  Nano Banana (gemini-2.5-flash-image) - Fast, high-quality image generation
                </p>

                {/* Current Status */}
                <div className="flex items-center gap-2 mb-3">
                  {config?.hasGeminiApiKey ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-primary" />
                      <span className="text-sm text-primary">API key configured</span>
                    </>
                  ) : (
                    <>
                      <AlertCircle className="h-4 w-4 text-secondary" />
                      <span className="text-sm text-secondary">No API key set</span>
                    </>
                  )}
                </div>

                {/* API Key Input */}
                <div className="space-y-2">
                  <input
                    type="password"
                    placeholder={config?.hasGeminiApiKey ? 'Enter new key to update...' : 'Enter your Gemini API key...'}
                    value={geminiApiKey}
                    onChange={(e) => setGeminiApiKey(e.target.value)}
                    className="w-full h-9 px-3 rounded-lg border border-border bg-card text-sm text-primary placeholder:text-accent focus:outline-none focus:ring-2 focus:ring-primary"
                  />
                  <Button
                    className="w-full bg-primary hover:bg-primary/90 text-background"
                    onClick={handleSaveGeminiApiKey}
                    disabled={savingGemini || !geminiApiKey.trim()}
                  >
                    {savingGemini ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      'Save Gemini API Key'
                    )}
                  </Button>
                </div>

                {successGemini && (
                  <div className="flex items-center gap-2 mt-2 text-primary text-sm">
                    <CheckCircle className="h-4 w-4" />
                    API key saved successfully!
                  </div>
                )}

                {/* Get API Key Link */}
                <a
                  href="https://aistudio.google.com/app/apikey"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-1 text-xs text-primary hover:underline mt-3"
                >
                  Get your API key at Google AI Studio
                  <ExternalLink className="h-3 w-3" />
                </a>
              </Card>

              {/* fal.ai API Key Section - SECONDARY */}
              <Card className="p-4 bg-card border-border">
                <div className="flex items-center gap-2 mb-3">
                  <Key className="h-4 w-4 text-primary" />
                  <Label className="text-sm font-medium text-primary">fal.ai API Key</Label>
                  <span className="text-xs text-accent bg-card px-2 py-0.5 rounded-full border border-border">Optional</span>
                </div>

                <p className="text-xs text-secondary mb-3">
                  Required for Multi-Angle and Upscale features (Qwen, Topaz)
                </p>

                {/* Current Status */}
                <div className="flex items-center gap-2 mb-3">
                  {config?.hasFalApiKey ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-primary" />
                      <span className="text-sm text-primary">API key configured</span>
                    </>
                  ) : (
                    <>
                      <AlertCircle className="h-4 w-4 text-accent" />
                      <span className="text-sm text-accent">No API key set</span>
                    </>
                  )}
                </div>

                {/* API Key Input */}
                <div className="space-y-2">
                  <input
                    type="password"
                    placeholder={config?.hasFalApiKey ? 'Enter new key to update...' : 'Enter your fal.ai API key...'}
                    value={falApiKey}
                    onChange={(e) => setFalApiKey(e.target.value)}
                    className="w-full h-9 px-3 rounded-lg border border-border bg-card text-sm text-primary placeholder:text-accent focus:outline-none focus:ring-2 focus:ring-primary"
                  />
                  <Button
                    className="w-full"
                    variant="outline"
                    onClick={handleSaveFalApiKey}
                    disabled={savingFal || !falApiKey.trim()}
                  >
                    {savingFal ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      'Save fal.ai API Key'
                    )}
                  </Button>
                </div>

                {successFal && (
                  <div className="flex items-center gap-2 mt-2 text-primary text-sm">
                    <CheckCircle className="h-4 w-4" />
                    API key saved successfully!
                  </div>
                )}

                {/* Get API Key Link */}
                <a
                  href="https://fal.ai/dashboard/keys"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-1 text-xs text-primary hover:underline mt-3"
                >
                  Get your API key at fal.ai
                  <ExternalLink className="h-3 w-3" />
                </a>
              </Card>

              {/* Data Path Info */}
              {config && (
                <Card className="p-4 bg-card/50 border-border">
                  <Label className="text-xs text-accent">Data Location</Label>
                  <p className="text-sm font-mono text-secondary break-all">{config.dataPath}</p>
                </Card>
              )}

              {/* Error Message */}
              {error && (
                <div className="flex items-center gap-2 p-3 rounded-lg bg-accent/10 text-accent text-sm">
                  <AlertCircle className="h-4 w-4" />
                  {error}
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-2 p-4 border-t border-border">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </div>
  );
}
