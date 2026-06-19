import { ChevronDown } from 'lucide-react';
import { AVAILABLE_MODELS, MODELS, ModeType } from '@/lib/models';

interface ModelSelectorProps {
  mode: ModeType;
  selectedModelId: string;
  onChange: (modelId: string) => void;
}

export function InspectorModelSelector({ mode, selectedModelId, onChange }: ModelSelectorProps) {
  const models = AVAILABLE_MODELS[mode];
  const currentModelInfo = MODELS[selectedModelId];

  return (
    <div className="relative mb-4 group">
      <label className="text-micro font-bold text-accent uppercase tracking-wider mb-1.5 block">
        AI Model
      </label>
      <div className="relative">
        <select
          value={selectedModelId}
          onChange={(event) => onChange(event.target.value)}
          className="w-full appearance-none bg-card border border-border text-primary text-sm rounded-lg px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-primary"
        >
          {models.map((id) => (
            <option key={id} value={id}>
              {MODELS[id]?.name || id}
            </option>
          ))}
        </select>
        <ChevronDown className="absolute right-3 top-3 h-4 w-4 text-secondary pointer-events-none" />
      </div>
      {currentModelInfo && (
        <p className="mt-1.5 text-micro text-secondary leading-relaxed">
          {currentModelInfo.description}
        </p>
      )}
    </div>
  );
}
