import React, { useId, useRef } from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './Button';
import { useDialogA11y } from '@/hooks/useDialogA11y';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  className?: string;
}

export function Modal({ isOpen, onClose, title, children, className }: ModalProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  useDialogA11y(isOpen, dialogRef, onClose);

  if (!isOpen) return null;

  return (
    <div className="modal-backdrop fixed inset-0 z-50 bg-background/80 backdrop-blur-sm">
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className="modal-panel fixed left-[50%] top-[50%] z-50 grid w-full max-w-lg translate-x-[-50%] translate-y-[-50%] gap-4 border border-border bg-panel p-6 shadow-lg duration-200 sm:rounded-lg"
      >
        <div className="flex flex-col space-y-1.5 text-center sm:text-left">
          <div className="flex items-center justify-between">
            <h2 id={titleId} className="text-lg font-semibold leading-none tracking-tight text-primary">{title}</h2>
            <Button variant="ghost" size="icon" onClick={onClose} className="h-6 w-6" aria-label="Close dialog">
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>
        <div className={cn("pt-2", className)}>
          {children}
        </div>
      </div>
    </div>
  );
}
