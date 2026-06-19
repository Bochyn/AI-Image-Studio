// ============================================================================
// BrushEngine — Pure TypeScript canvas drawing engine for mask painting
// Uses quadratic Bezier interpolation for smooth strokes
// Operates on source resolution (not screen resolution)
// ============================================================================

export class BrushEngine {
  private ctx: CanvasRenderingContext2D;
  private points: { x: number; y: number }[] = [];
  private isDrawing = false;

  /** Brush color (hex string). Applied via strokeStyle. */
  color: string = '#ef4444';

  /** Brush size in source-resolution pixels. */
  size: number = 20;

  /** Drawing mode: 'brush' paints, 'eraser' removes. */
  mode: 'brush' | 'eraser' = 'brush';

  constructor(ctx: CanvasRenderingContext2D) {
    this.ctx = ctx;
  }

  /** Returns the current canvas rendering context. */
  getContext(): CanvasRenderingContext2D {
    return this.ctx;
  }

  /**
   * Begin a new stroke at the given source-resolution coordinates.
   */
  beginStroke(x: number, y: number): void {
    this.isDrawing = true;
    this.points = [{ x, y }];

    this.applyContextSettings();

    // Draw a single dot at the start point so single-click paints something
    this.ctx.beginPath();
    this.ctx.arc(x, y, this.size / 2, 0, Math.PI * 2);
    this.ctx.fill();
  }

  /**
   * Continue the current stroke to a new point.
   * Uses quadratic Bezier interpolation with midpoints for smooth curves.
   */
  continueStroke(x: number, y: number): void {
    if (!this.isDrawing) return;

    this.points.push({ x, y });

    const len = this.points.length;

    if (len < 3) {
      // Not enough points for Bezier yet — draw a simple line segment
      const p0 = this.points[len - 2];
      const p1 = this.points[len - 1];

      this.applyContextSettings();
      this.ctx.beginPath();
      this.ctx.moveTo(p0.x, p0.y);
      this.ctx.lineTo(p1.x, p1.y);
      this.ctx.stroke();
      return;
    }

    // Quadratic Bezier through midpoints:
    // For the last 3 points (p0, p1, p2), draw a quadratic curve
    // from midpoint(p0,p1) through p1 to midpoint(p1,p2).
    const p0 = this.points[len - 3];
    const p1 = this.points[len - 2];
    const p2 = this.points[len - 1];

    const mid0x = (p0.x + p1.x) / 2;
    const mid0y = (p0.y + p1.y) / 2;
    const mid1x = (p1.x + p2.x) / 2;
    const mid1y = (p1.y + p2.y) / 2;

    this.applyContextSettings();
    this.ctx.beginPath();
    this.ctx.moveTo(mid0x, mid0y);
    this.ctx.quadraticCurveTo(p1.x, p1.y, mid1x, mid1y);
    this.ctx.stroke();

    // Trim points array to last 3 to prevent unbounded growth during long strokes
    if (this.points.length > 3) {
      this.points = this.points.slice(-3);
    }
  }

  /**
   * End the current stroke.
   */
  endStroke(): void {
    if (!this.isDrawing) return;

    // Draw final segment to the last point if we have enough points
    if (this.points.length >= 2) {
      const lastIdx = this.points.length - 1;
      const prevPoint = this.points[lastIdx - 1];
      const lastPoint = this.points[lastIdx];

      const midX = (prevPoint.x + lastPoint.x) / 2;
      const midY = (prevPoint.y + lastPoint.y) / 2;

      this.applyContextSettings();
      this.ctx.beginPath();
      this.ctx.moveTo(midX, midY);
      this.ctx.lineTo(lastPoint.x, lastPoint.y);
      this.ctx.stroke();
    }

    this.isDrawing = false;
    this.points = [];
  }

  /**
   * Clear the entire canvas.
   */
  clear(): void {
    this.ctx.clearRect(0, 0, this.ctx.canvas.width, this.ctx.canvas.height);
  }

  /**
   * Get a snapshot of the current canvas pixel data.
   */
  getImageData(): ImageData {
    return this.ctx.getImageData(0, 0, this.ctx.canvas.width, this.ctx.canvas.height);
  }

  /**
   * Restore canvas from an ImageData snapshot.
   */
  putImageData(data: ImageData): void {
    this.ctx.putImageData(data, 0, 0);
  }

  /**
   * Apply current brush settings to the canvas context.
   */
  private applyContextSettings(): void {
    this.ctx.lineCap = 'round';
    this.ctx.lineJoin = 'round';
    this.ctx.lineWidth = this.size;

    if (this.mode === 'eraser') {
      this.ctx.globalCompositeOperation = 'destination-out';
      // Eraser uses full opacity regardless of color
      this.ctx.strokeStyle = 'rgba(0,0,0,1)';
      this.ctx.fillStyle = 'rgba(0,0,0,1)';
    } else {
      this.ctx.globalCompositeOperation = 'source-over';
      this.ctx.strokeStyle = this.color;
      this.ctx.fillStyle = this.color;
    }
  }
}
