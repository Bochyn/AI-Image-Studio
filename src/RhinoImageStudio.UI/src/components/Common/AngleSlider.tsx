import { cn } from "@/lib/utils";

interface AngleSliderProps {
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  step?: number;
  label: string;
  valueDisplay?: string;
  showCenterMark?: boolean;
  centerValue?: number;
  minLabel?: string;
  maxLabel?: string;
  className?: string;
}

export function AngleSlider({
  value,
  onChange,
  min,
  max,
  step = 1,
  label,
  valueDisplay,
  showCenterMark = false,
  centerValue = 0,
  minLabel,
  maxLabel,
  className,
}: AngleSliderProps) {
  const percentage = ((value - min) / (max - min)) * 100;
  const centerPercentage = ((centerValue - min) / (max - min)) * 100;

  return (
    <div className={cn("w-full space-y-1.5", className)}>
      {/* Label Row */}
      <div className="flex justify-between items-center">
        <label className="text-micro font-bold text-accent uppercase tracking-wider">
          {label}
        </label>
        {valueDisplay && (
          <span className="text-micro font-medium text-primary tabular-nums">
            {valueDisplay}
          </span>
        )}
      </div>

      {/* Slider Track */}
      <div className="relative flex items-center h-6 select-none touch-none w-full">
        <input
          type="range"
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={(e) => onChange(Number(e.target.value))}
          className="absolute w-full h-full opacity-0 z-20 cursor-pointer"
        />

        {/* Track Background */}
        <div className="relative w-full h-2 bg-border rounded-full overflow-hidden">
          {/* Fill - from center for bipolar sliders, from left for unipolar */}
          {showCenterMark ? (
            // Bipolar: fill from center
            <div
              className="absolute h-full bg-primary/40 transition-all duration-150"
              style={{
                left: value >= centerValue
                  ? `${centerPercentage}%`
                  : `${percentage}%`,
                width: value >= centerValue
                  ? `${percentage - centerPercentage}%`
                  : `${centerPercentage - percentage}%`,
              }}
            />
          ) : (
            // Unipolar: fill from left
            <div
              className="absolute h-full bg-primary/40 transition-all duration-150"
              style={{ width: `${percentage}%` }}
            />
          )}

          {/* Center Mark */}
          {showCenterMark && (
            <div
              className="absolute top-0 w-0.5 h-full bg-accent/60"
              style={{ left: `${centerPercentage}%` }}
            />
          )}
        </div>

        {/* Thumb */}
        <div
          className="absolute h-4 w-4 bg-primary rounded-full shadow-md transition-all duration-150 pointer-events-none z-10 border-2 border-background"
          style={{ left: `calc(${percentage}% - 8px)` }}
        />
      </div>

      {/* End Labels */}
      {(minLabel || maxLabel) && (
        <div className="flex justify-between text-[9px] text-accent px-0.5">
          <span>{minLabel}</span>
          <span>{maxLabel}</span>
        </div>
      )}
    </div>
  );
}
