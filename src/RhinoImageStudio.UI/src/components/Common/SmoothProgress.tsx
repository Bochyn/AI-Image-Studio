import { useState, useEffect, useRef } from 'react';

interface SmoothProgressProps {
  targetValue: number;
  className?: string;
  duration?: number; // ms to animate from current to target
}

/**
 * A progress bar that smoothly interpolates to the target value
 * instead of jumping directly. Uses easing for natural feel.
 */
export function SmoothProgress({
  targetValue,
  className = '',
  duration = 800
}: SmoothProgressProps) {
  const [displayValue, setDisplayValue] = useState(targetValue);
  const animationRef = useRef<number | null>(null);
  const startValueRef = useRef(targetValue);
  const startTimeRef = useRef<number | null>(null);

  useEffect(() => {
    // Cancel any ongoing animation
    if (animationRef.current) {
      cancelAnimationFrame(animationRef.current);
    }

    startValueRef.current = displayValue;
    startTimeRef.current = null;

    const animate = (timestamp: number) => {
      if (!startTimeRef.current) {
        startTimeRef.current = timestamp;
      }

      const elapsed = timestamp - startTimeRef.current;
      const progress = Math.min(elapsed / duration, 1);

      // Ease-out cubic for smooth deceleration
      const eased = 1 - Math.pow(1 - progress, 3);

      const currentValue = startValueRef.current + (targetValue - startValueRef.current) * eased;
      setDisplayValue(Math.round(currentValue));

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate);
      }
    };

    animationRef.current = requestAnimationFrame(animate);

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [targetValue, duration]);

  return (
    <div className={`w-full space-y-2 ${className}`}>
      <div className="relative h-2 bg-primary/10 rounded-full overflow-hidden">
        <div
          className="absolute inset-y-0 left-0 bg-gradient-to-r from-primary to-primary/70 rounded-full"
          style={{ width: `${displayValue}%` }}
        />
        {/* Shimmer effect */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-primary/20 to-transparent animate-shimmer" />
      </div>
      <div className="flex justify-between items-center text-xs">
        <span className="text-accent">Progress</span>
        <span className="text-primary font-medium tabular-nums">
          {displayValue}%
        </span>
      </div>
    </div>
  );
}
