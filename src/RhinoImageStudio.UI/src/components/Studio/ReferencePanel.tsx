import { useRef, useState } from 'react';
import { ImagePlus, X, Loader2 } from 'lucide-react';
import { Button } from '@/components/Common/Button';
import { api } from '@/lib/api';
import { ReferenceImage } from '@/lib/types';

interface ReferencePanelProps {
  projectId: string;
  references: ReferenceImage[];
  maxReferences: number;
  onReferencesChange: (refs: ReferenceImage[]) => void;
  onClose: () => void;
}

export function ReferencePanel({
  projectId,
  references,
  maxReferences,
  onReferencesChange,
  onClose,
}: ReferencePanelProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);

  const canAdd = references.length < maxReferences && !isUploading;

  const handleFiles = async (files: FileList | File[]) => {
    const imageFiles = Array.from(files).filter(f => f.type.startsWith('image/'));
    const remaining = maxReferences - references.length;
    const toUpload = imageFiles.slice(0, remaining);

    if (toUpload.length === 0) return;

    setIsUploading(true);
    let currentRefs = [...references];
    try {
      for (const file of toUpload) {
        const ref = await api.references.upload(projectId, file);
        currentRefs = [...currentRefs, ref];
        onReferencesChange(currentRefs);
      }
    } catch (error) {
      console.error('Failed to upload reference:', error);
    } finally {
      setIsUploading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.references.delete(id);
      onReferencesChange(references.filter(r => r.id !== id));
    } catch (error) {
      console.error('Failed to delete reference:', error);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    if (canAdd && e.dataTransfer.files.length > 0) {
      handleFiles(e.dataTransfer.files);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    if (canAdd) setIsDragOver(true);
  };

  const handleDragLeave = () => setIsDragOver(false);

  return (
    <div
      className={`flex-shrink-0 bg-card border border-border rounded-xl p-3 transition-colors ${
        isDragOver ? 'border-primary bg-primary/5' : ''
      }`}
      onDrop={handleDrop}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
    >
      <div className="flex items-center gap-3">
        {/* Header */}
        <div className="flex items-center gap-2 flex-shrink-0">
          <span className="text-micro font-bold text-accent uppercase tracking-wider">References</span>
          <span className="text-micro text-secondary">{references.length}/{maxReferences}</span>
        </div>

        {/* Thumbnails */}
        <div className="flex items-center gap-2 flex-1 min-w-0 overflow-x-auto">
          {references.map((ref) => (
            <div key={ref.id} className="relative group flex-shrink-0">
              <img
                src={ref.thumbnailUrl || ref.imageUrl}
                alt={ref.originalFileName}
                className="w-12 h-12 rounded-lg object-cover border border-border"
              />
              <button
                onClick={() => handleDelete(ref.id)}
                className="absolute -top-1.5 -right-1.5 w-5 h-5 rounded-full bg-danger text-background flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
              >
                <X className="h-3 w-3" />
              </button>
            </div>
          ))}

          {/* Add button */}
          {canAdd && (
            <button
              onClick={() => fileInputRef.current?.click()}
              className="w-12 h-12 rounded-lg border-2 border-dashed border-border hover:border-primary flex items-center justify-center text-secondary hover:text-primary transition-colors flex-shrink-0"
            >
              {isUploading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <ImagePlus className="h-4 w-4" />
              )}
            </button>
          )}
        </div>

        {/* Close button */}
        <Button
          variant="ghost"
          size="icon"
          onClick={onClose}
          className="h-7 w-7 rounded-full hover:bg-primary/10 text-secondary flex-shrink-0"
        >
          <X className="h-3.5 w-3.5" />
        </Button>
      </div>

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        multiple
        className="hidden"
        onChange={(e) => {
          if (e.target.files) handleFiles(e.target.files);
          e.target.value = '';
        }}
      />
    </div>
  );
}
