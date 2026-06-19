import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatDistanceToNow } from 'date-fns';
import { api } from '@/lib/api';
import { Generation } from '@/lib/types';
import { Button } from '@/components/Common/Button';
import { Image } from 'lucide-react';

interface GenerationGalleryProps {
  isActive: boolean;
}

const PAGE_SIZE = 50;

function truncate(text: string | null | undefined, max = 120): string {
  if (!text) return 'No prompt';
  if (text.length <= max) return text;
  return `${text.slice(0, max - 1)}...`;
}

export function GenerationGallery({ isActive }: GenerationGalleryProps) {
  const navigate = useNavigate();
  const [items, setItems] = useState<Generation[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [isLoadedOnce, setIsLoadedOnce] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasMore = useMemo(() => items.length < total, [items.length, total]);

  const loadPage = async (offset: number, append: boolean) => {
    if (append) {
      setIsLoadingMore(true);
    } else {
      setIsLoading(true);
    }

    try {
      const data = await api.generations.listGlobal(PAGE_SIZE, offset);
      setTotal(data.total);
      setItems((prev) => (append ? [...prev, ...data.generations] : data.generations));
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load generations';
      setError(message);
    } finally {
      setIsLoading(false);
      setIsLoadingMore(false);
    }
  };

  useEffect(() => {
    if (!isActive || isLoadedOnce) return;
    setIsLoadedOnce(true);
    void loadPage(0, false);
  }, [isActive, isLoadedOnce]);

  if (!isActive) {
    return null;
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="masonry-grid">
          {Array.from({ length: 8 }).map((_, index) => (
            <div key={index} className="masonry-item rounded-lg border border-border bg-card p-3">
              <div className="mb-3 h-40 animate-pulse rounded bg-card-hover" />
              <div className="mb-2 h-4 w-2/3 animate-pulse rounded bg-card-hover" />
              <div className="h-3 w-1/2 animate-pulse rounded bg-card-hover" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-dashed border-border p-8 text-center">
        <p className="text-danger">{error}</p>
        <Button className="mt-4" onClick={() => void loadPage(0, false)}>
          Retry
        </Button>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="flex h-64 flex-col items-center justify-center rounded-lg border border-dashed border-border text-center">
        <Image className="mb-4 h-12 w-12 text-secondary" />
        <p className="text-secondary">No generations yet</p>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="masonry-grid">
        {items.map((generation) => {
          const previewUrl = generation.thumbnailUrl || generation.imageUrl;
          const aspectRatio =
            generation.width && generation.height
              ? `${generation.width} / ${generation.height}`
              : '1 / 1';

          return (
            <button
              key={generation.id}
              type="button"
              className="masonry-item w-full rounded-lg border border-border bg-card p-2 text-left transition-colors hover:bg-card-hover"
              onClick={() => navigate(`/project/${generation.projectId}?gen=${generation.id}`)}
            >
              {previewUrl ? (
                <img
                  src={previewUrl}
                  alt={truncate(generation.prompt, 40)}
                  className="mb-3 w-full rounded-md object-cover"
                  style={{ aspectRatio }}
                />
              ) : (
                <div className="mb-3 flex w-full items-center justify-center rounded-md bg-background text-secondary" style={{ aspectRatio }}>
                  No Preview
                </div>
              )}

              <p className="truncate text-sm font-semibold text-primary">
                {generation.projectName || 'Unknown project'}
              </p>
              <p className="mt-1 text-xs text-secondary">
                {formatDistanceToNow(new Date(generation.createdAt), { addSuffix: true })}
              </p>
              <p className="mt-2 text-sm text-text">{truncate(generation.prompt)}</p>
            </button>
          );
        })}
      </div>

      {hasMore && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            isLoading={isLoadingMore}
            onClick={() => void loadPage(items.length, true)}
          >
            Load more
          </Button>
        </div>
      )}
    </div>
  );
}
