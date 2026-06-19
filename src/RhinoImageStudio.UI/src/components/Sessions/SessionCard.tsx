import { Project } from '@/lib/types';
import { Card, CardContent } from '@/components/Common/Card';
import { Button } from '@/components/Common/Button';
import { Pin, Trash2, Calendar } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { cn } from '@/lib/utils';
import { useNavigate } from 'react-router-dom';
import { useEffect, useRef, useState } from 'react';

interface ProjectCardProps {
  project: Project;
  onPin: (id: string, current: boolean) => void;
  onRename: (id: string, newName: string) => Promise<void>;
  onDelete: (id: string) => void;
}

export function ProjectCard({ project, onPin, onRename, onDelete }: ProjectCardProps) {
  const navigate = useNavigate();
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [editName, setEditName] = useState(project.name);
  const inputRef = useRef<HTMLInputElement>(null);
  const clickTimeoutRef = useRef<ReturnType<typeof setTimeout>>();

  useEffect(() => {
    setEditName(project.name);
  }, [project.name]);

  useEffect(() => {
    if (isEditing) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [isEditing]);

  useEffect(() => {
    return () => {
      clearTimeout(clickTimeoutRef.current);
    };
  }, []);

  const handleCardClick = () => {
    if (isEditing) return;
    clearTimeout(clickTimeoutRef.current);
    clickTimeoutRef.current = setTimeout(() => {
      navigate(`/project/${project.id}`);
    }, 250);
  };

  const startEditing = (e: React.MouseEvent) => {
    e.stopPropagation();
    clearTimeout(clickTimeoutRef.current);
    setEditName(project.name);
    setIsEditing(true);
  };

  const cancelEditing = () => {
    setEditName(project.name);
    setIsEditing(false);
  };

  const submitRename = async () => {
    if (isSaving) return;

    const trimmedName = editName.trim();
    if (trimmedName.length < 1 || trimmedName.length > 100) {
      cancelEditing();
      return;
    }

    if (trimmedName === project.name) {
      setIsEditing(false);
      return;
    }

    setIsSaving(true);
    try {
      await onRename(project.id, trimmedName);
      setIsEditing(false);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Card
      className="group relative overflow-hidden transition-all hover:ring-2 hover:ring-primary cursor-pointer"
      onClick={handleCardClick}
    >
      <div className="aspect-video w-full bg-card overflow-hidden relative">
        {project.previewUrl ? (
          <img
            src={project.previewUrl}
            alt={project.name}
            className="h-full w-full object-cover transition-transform group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center bg-card text-secondary">
            No Preview
          </div>
        )}
        <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity flex gap-1">
          <Button
            variant="secondary"
            size="icon"
            className="h-8 w-8 bg-background/70 hover:bg-background/90 text-primary border-0"
            onClick={(e) => {
              e.stopPropagation();
              onPin(project.id, project.isPinned);
            }}
          >
            <Pin className={cn("h-4 w-4", project.isPinned && "fill-current")} />
          </Button>
          <Button
            variant="destructive"
            size="icon"
            className="h-8 w-8"
            onClick={(e) => {
              e.stopPropagation();
              onDelete(project.id);
            }}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <CardContent className="p-4">
        {isEditing ? (
          <input
            ref={inputRef}
            type="text"
            maxLength={100}
            value={editName}
            disabled={isSaving}
            className="w-full rounded border border-border bg-background px-2 py-1 text-sm text-primary focus:outline-none focus:ring-2 focus:ring-primary"
            onClick={(e) => e.stopPropagation()}
            onChange={(e) => setEditName(e.target.value)}
            onBlur={submitRename}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                void submitRename();
              } else if (e.key === 'Escape') {
                e.preventDefault();
                cancelEditing();
              }
            }}
          />
        ) : (
          <h3
            className="font-semibold truncate text-primary"
            onDoubleClick={startEditing}
            title="Double-click to rename"
          >
            {project.name}
          </h3>
        )}
        <div className="flex items-center gap-2 text-xs text-secondary mt-1">
          <Calendar className="h-3 w-3" />
          <span>{formatDistanceToNow(new Date(project.updatedAt), { addSuffix: true })}</span>
        </div>
      </CardContent>
    </Card>
  );
}
