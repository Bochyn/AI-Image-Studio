// ============================================================================
// MaskHistory — Undo/Redo via ImageData snapshots
// Each instance is per mask layer.
// Memory budget: ~80 MB for 20 steps at 1K, ~640 MB for 10 steps at 4K.
// ============================================================================

export class MaskHistory {
  private undoStack: ImageData[] = [];
  private redoStack: ImageData[] = [];
  private maxSteps: number;

  /**
   * @param maxSteps Maximum number of undo steps to retain.
   *   Recommended: 20 for 1K resolution (~80 MB), 10 for 4K (~640 MB).
   */
  constructor(maxSteps: number = 20) {
    this.maxSteps = maxSteps;
  }

  /**
   * Push a new canvas state onto the undo stack.
   * Clears the redo stack (new action invalidates redo history).
   * Evicts the oldest entry when maxSteps is exceeded.
   */
  pushState(imageData: ImageData): void {
    this.undoStack.push(imageData);

    // Evict oldest entries beyond the limit
    while (this.undoStack.length > this.maxSteps) {
      this.undoStack.shift();
    }

    // New action invalidates any redo history
    this.redoStack = [];
  }

  /**
   * Undo the last action and return the previous state.
   * Returns null if no undo history is available.
   */
  undo(): ImageData | null {
    if (this.undoStack.length === 0) return null;

    const current = this.undoStack.pop()!;
    this.redoStack.push(current);

    // Return the state that is now on top (the one to restore),
    // or null if the stack is now empty (caller should clear canvas)
    if (this.undoStack.length === 0) return null;
    return this.undoStack[this.undoStack.length - 1];
  }

  /**
   * Redo the last undone action and return the restored state.
   * Returns null if no redo history is available.
   */
  redo(): ImageData | null {
    if (this.redoStack.length === 0) return null;

    const state = this.redoStack.pop()!;
    this.undoStack.push(state);

    return state;
  }

  /**
   * Whether undo is available.
   */
  canUndo(): boolean {
    return this.undoStack.length > 0;
  }

  /**
   * Whether redo is available.
   */
  canRedo(): boolean {
    return this.redoStack.length > 0;
  }

  /**
   * Clear all history (both undo and redo).
   */
  clear(): void {
    this.undoStack = [];
    this.redoStack = [];
  }
}
