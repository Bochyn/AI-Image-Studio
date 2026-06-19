import { Sparkles, RefreshCw, Move3D, ArrowUpCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ModeType } from '@/lib/models';

interface ModeSelectorProps {
  mode: ModeType;
  onChange: (mode: ModeType) => void;
}

export function InspectorModeSelector({ mode, onChange }: ModeSelectorProps) {
  return (
    <div className="grid grid-cols-4 gap-1 p-1 bg-card/50 rounded-lg mb-6 border border-border/50">
      {[
        { id: 'generate', icon: Sparkles, label: 'Gen' },
        { id: 'refine', icon: RefreshCw, label: 'Edit' },
        { id: 'multiangle', icon: Move3D, label: 'Pan' },
        { id: 'upscale', icon: ArrowUpCircle, label: 'Up' },
      ].map((item) => (
        <button
          key={item.id}
          onClick={() => onChange(item.id as ModeType)}
          className={cn(
            'flex flex-col items-center justify-center gap-1 py-2 rounded-md transition-all duration-200',
            mode === item.id
              ? 'bg-card text-primary shadow-lg ring-1 ring-border'
              : 'text-accent hover:text-primary hover:bg-primary/5'
          )}
        >
          <item.icon className="h-4 w-4" />
          <span className="text-micro font-medium">{item.label}</span>
        </button>
      ))}
    </div>
  );
}
