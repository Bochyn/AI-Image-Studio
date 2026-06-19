import React from "react";
import { cn } from "@/lib/utils";

interface SliderProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  value: number;
  min: number;
  max: number;
  step?: number;
  onChange: (value: number) => void;
  label?: string;
  suffix?: string;
}

const Slider = React.forwardRef<HTMLInputElement, SliderProps>(
  ({ className, value, min, max, step = 1, onChange, label, suffix, ...props }, ref) => {

    const percentage = ((value - min) / (max - min)) * 100;

    return (
      <div className={cn("w-full space-y-2", className)}>
        {(label || suffix) && (
          <div className="flex justify-between text-xs text-secondary">
            {label && <span>{label}</span>}
            {suffix && <span>{value}{suffix}</span>}
          </div>
        )}
        <div className="relative flex items-center h-5 select-none touch-none w-full">
            <input
                type="range"
                min={min}
                max={max}
                step={step}
                value={value}
                onChange={(e) => onChange(Number(e.target.value))}
                ref={ref}
                className="absolute w-full h-2 opacity-0 z-10 cursor-pointer"
                {...props}
            />
            <div className="relative w-full h-2 bg-secondary rounded-full overflow-hidden">
                <div
                    className="absolute h-full bg-primary transition-all"
                    style={{ width: `${percentage}%` }}
                />
            </div>
            <div
                className="absolute h-4 w-4 bg-background border-2 border-primary rounded-full shadow transition-all pointer-events-none"
                style={{ left: `calc(${percentage}% - 8px)` }}
            />
        </div>
      </div>
    );
  }
);
Slider.displayName = "Slider";

export { Slider };
