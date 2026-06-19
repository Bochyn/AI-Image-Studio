import { useState, useEffect, useId, useRef } from 'react';
import { api } from '@/lib/api';
import { GenerationDebugInfo } from '@/lib/types';
import { X, Bug, Copy, Check, Code } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useDialogA11y } from '@/hooks/useDialogA11y';

interface DebugModalProps {
  isOpen: boolean;
  onClose: () => void;
  generationId: string | null;
}

export function DebugModal({ isOpen, onClose, generationId }: DebugModalProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  useDialogA11y(isOpen, dialogRef, onClose);

  const [debugInfo, setDebugInfo] = useState<GenerationDebugInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [showRawJson, setShowRawJson] = useState(false);

  useEffect(() => {
    if (isOpen && generationId) {
      setLoading(true);
      setError(null);
      setDebugInfo(null);
      setCopied(false);
      setShowRawJson(false);

      api.generations.getDebugInfo(generationId)
        .then((data) => {
          setDebugInfo(data);
        })
        .catch((err) => {
          setError(err.message || 'Failed to load debug info');
        })
        .finally(() => {
          setLoading(false);
        });
    }
  }, [isOpen, generationId]);

  const handleCopyJson = async () => {
    if (!debugInfo) return;
    try {
      await navigator.clipboard.writeText(JSON.stringify(debugInfo, null, 2));
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      console.error('Failed to copy to clipboard');
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="modal-backdrop absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      {/* Panel */}
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className="modal-panel relative bg-panel border border-border rounded-2xl max-w-lg w-full max-h-[80vh] overflow-y-auto mx-4"
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border sticky top-0 bg-panel rounded-t-2xl z-10">
          <div className="flex items-center gap-2">
            <Bug className="h-4 w-4 text-primary" />
            <h2 id={titleId} className="text-lg font-semibold text-primary">Request Debug</h2>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-full hover:bg-card transition-colors"
            aria-label="Close debug dialog"
          >
            <X className="h-4 w-4 text-secondary" />
          </button>
        </div>

        {/* Content */}
        <div className="p-4 space-y-4">
          {loading && (
            <div className="space-y-3 py-2">
              <div className="h-4 w-28 animate-pulse rounded bg-card-hover" />
              <div className="h-24 w-full animate-pulse rounded-lg bg-card-hover" />
              <div className="h-4 w-20 animate-pulse rounded bg-card-hover" />
              <div className="h-16 w-full animate-pulse rounded-lg bg-card-hover" />
              <div className="h-4 w-24 animate-pulse rounded bg-card-hover" />
              <div className="h-16 w-full animate-pulse rounded-lg bg-card-hover" />
            </div>
          )}

          {error && (
            <div className="flex items-center gap-2 p-3 rounded-lg bg-accent/10 text-accent text-sm">
              {error}
            </div>
          )}

          {debugInfo && !showRawJson && (
            <>
              {/* Prompt */}
              <Section label="Prompt">
                <pre className="text-sm text-primary bg-card rounded-lg p-3 max-h-32 overflow-y-auto whitespace-pre-wrap break-words font-mono border border-border">
                  {debugInfo.prompt || '(empty)'}
                </pre>
              </Section>

              {/* Augmented Prompt (sent to Gemini) */}
              {debugInfo.augmentedPrompt && (
                <Section label="Augmented Prompt (sent to AI)">
                  <pre className="text-sm text-primary bg-card rounded-lg p-3 max-h-48 overflow-y-auto whitespace-pre-wrap break-words font-mono border border-border">
                    {debugInfo.augmentedPrompt}
                  </pre>
                </Section>
              )}

              {/* Model */}
              <Section label="Model">
                <Value>{debugInfo.model || '(unknown)'}</Value>
              </Section>

              {/* Settings */}
              <Section label="Settings">
                <div className="grid grid-cols-2 gap-2">
                  <KeyValue label="Aspect Ratio" value={debugInfo.aspectRatio || '-'} />
                  <KeyValue label="Resolution" value={debugInfo.resolution || '-'} />
                  <KeyValue label="Output Format" value={debugInfo.outputFormat || '-'} />
                  <KeyValue label="Num Images" value={String(debugInfo.numImages ?? '-')} />
                </div>
              </Section>

              {/* Source */}
              <Section label="Source">
                <div className="grid grid-cols-2 gap-2">
                  <KeyValue label="Type" value={debugInfo.sourceType || '-'} />
                  <KeyValue label="ID" value={debugInfo.sourceId ? truncateId(debugInfo.sourceId) : '-'} />
                </div>
              </Section>

              {/* References */}
              <Section label="References">
                <Value>
                  {debugInfo.referenceCount > 0
                    ? `${debugInfo.referenceCount} reference(s)`
                    : 'None'}
                </Value>
                {debugInfo.referenceDetails && debugInfo.referenceDetails.length > 0 && (
                  <div className="mt-1.5 space-y-1">
                    {debugInfo.referenceDetails.map((ref, i) => (
                      <ReferenceRow key={ref.id} index={i} ref_={ref} />
                    ))}
                  </div>
                )}
              </Section>

              {/* Masks (legacy B&W format) */}
              {debugInfo.masks && debugInfo.masks.length > 0 && (
                <Section label={`Masks (${debugInfo.masks.length})`}>
                  <div className="space-y-1.5">
                    {debugInfo.masks.map((mask, i) => (
                      <MaskRow key={i} mask={mask} index={i} generationId={generationId!} />
                    ))}
                  </div>
                </Section>
              )}

              {/* Mask Overlay (new color format) */}
              {debugInfo.maskOverlay && (
                <div>
                  <h4 className="text-micro font-bold text-accent uppercase tracking-wider mb-2">
                    Mask Overlay
                  </h4>
                  <div className="space-y-2">
                    <div className="text-xs text-secondary">
                      Overlay image: {debugInfo.maskOverlay.overlayImageSize}
                    </div>
                    {/* Overlay thumbnail on hover */}
                    <div className="relative group inline-block">
                      <span className="text-xs text-primary cursor-pointer underline">
                        Preview overlay
                      </span>
                      <div className="hidden group-hover:block absolute left-0 top-6 z-50 p-1 bg-card border border-border rounded-lg shadow-xl">
                        <img
                          src={`/api/generations/${generationId}/masks/overlay`}
                          className="max-w-[200px] max-h-[200px] rounded"
                          alt="Mask overlay"
                        />
                      </div>
                    </div>
                    {/* Layer list */}
                    {debugInfo.maskOverlay.layers.map((layer, i) => (
                      <div key={i} className="flex items-center gap-2 text-xs">
                        <div
                          className="w-3 h-3 rounded-full border border-border"
                          style={{ backgroundColor: layer.color }}
                        />
                        <span className="text-secondary uppercase text-micro">{layer.colorName}</span>
                        <span className="text-text">{layer.instruction}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}

          {/* Raw JSON view */}
          {debugInfo && showRawJson && (
            <pre className="text-xs text-primary bg-card rounded-lg p-3 overflow-auto max-h-[60vh] whitespace-pre-wrap break-all font-mono border border-border">
              {formatRawJson(debugInfo.rawJson)}
            </pre>
          )}
        </div>

        {/* Footer */}
        {debugInfo && (
          <div className="flex justify-end gap-2 p-4 border-t border-border sticky bottom-0 bg-panel rounded-b-2xl">
            <button
              onClick={() => setShowRawJson(!showRawJson)}
              className={cn(
                "flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium transition-all border",
                showRawJson
                  ? "bg-primary/10 text-primary border-primary/30"
                  : "bg-card text-primary border-border hover:bg-card-hover"
              )}
            >
              <Code className="h-3.5 w-3.5" />
              {showRawJson ? 'Details' : 'View JSON'}
            </button>
            <button
              onClick={handleCopyJson}
              className={cn(
                "flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium transition-all border",
                copied
                  ? "bg-primary/10 text-primary border-primary/30"
                  : "bg-card text-primary border-border hover:bg-card-hover"
              )}
            >
              {copied ? (
                <>
                  <Check className="h-3.5 w-3.5" />
                  Copied
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5" />
                  Copy JSON
                </>
              )}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

// -- Helper components --

function Section({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-medium text-secondary uppercase tracking-wide mb-1.5">{label}</p>
      {children}
    </div>
  );
}

function Value({ children }: { children: React.ReactNode }) {
  return (
    <p className="text-sm text-primary">{children}</p>
  );
}

function KeyValue({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-card rounded-md px-2 py-1.5 border border-border">
      <p className="text-micro text-secondary">{label}</p>
      <p className="text-xs text-primary font-mono truncate" title={value}>{value}</p>
    </div>
  );
}

function ReferenceRow({ index, ref_ }: { index: number; ref_: { id: string; fileName: string; thumbnailUrl?: string } }) {
  const [showTooltip, setShowTooltip] = useState(false);

  return (
    <div
      className="relative flex items-center gap-2 group"
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <p className="text-xs text-accent font-mono">
        [{index}] {truncateId(ref_.id)}
      </p>
      <span className="text-micro text-secondary truncate max-w-[180px]" title={ref_.fileName}>
        {ref_.fileName}
      </span>

      {/* Tooltip with thumbnail */}
      {showTooltip && ref_.thumbnailUrl && (
        <div className="absolute left-0 bottom-full mb-1.5 z-20 bg-card border border-border rounded-lg p-1.5 shadow-lg">
          <img
            src={ref_.thumbnailUrl}
            alt={ref_.fileName}
            className="w-16 h-16 object-cover rounded"
          />
          <p className="text-micro text-secondary mt-1 max-w-[120px] truncate">{ref_.fileName}</p>
        </div>
      )}
    </div>
  );
}

function MaskRow({ mask, index, generationId }: {
  mask: { index: number; instruction: string; imageSize: string };
  index: number;
  generationId: string;
}) {
  const [showTooltip, setShowTooltip] = useState(false);

  return (
    <div
      className="relative bg-card rounded-lg p-2.5 border border-border"
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <p className="text-sm text-primary break-words">
        {mask.instruction || '(no instruction)'}
      </p>
      <p className="text-micro text-secondary mt-1 font-mono">
        #{mask.index} · {mask.imageSize}
      </p>

      {showTooltip && (
        <div className="absolute left-0 bottom-full mb-1.5 z-20 bg-card border border-border rounded-lg p-1.5 shadow-lg">
          <img
            src={`/api/generations/${generationId}/masks/${index}/image`}
            alt={`Mask ${mask.index}`}
            className="w-16 h-16 object-contain rounded bg-black"
          />
        </div>
      )}
    </div>
  );
}

function truncateId(id: string): string {
  if (id.length <= 12) return id;
  return `${id.slice(0, 8)}...${id.slice(-4)}`;
}

function formatRawJson(rawJson?: string): string {
  if (!rawJson) return '(no raw JSON available)';
  try {
    return JSON.stringify(JSON.parse(rawJson), null, 2);
  } catch {
    return rawJson;
  }
}
