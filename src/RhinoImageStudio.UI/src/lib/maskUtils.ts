// ============================================================================
// maskUtils — Mask export and compositing utilities
// ============================================================================

/**
 * Convert canvas ImageData to a binary PNG mask encoded as base64.
 * Pixels with alpha > 0 become white (255,255,255), rest become black (0,0,0).
 * Returns ONLY the base64 string (no "data:image/png;base64," prefix).
 */
export async function exportMaskAsPng(imageData: ImageData): Promise<string> {
  const { width, height, data } = imageData;

  // Create an offscreen canvas for the binary mask
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d')!;

  // Build a new ImageData where alpha > 0 => white, else => black
  const maskData = ctx.createImageData(width, height);
  const src = data;
  const dst = maskData.data;

  for (let i = 0; i < src.length; i += 4) {
    const alpha = src[i + 3]; // Alpha channel of the painted mask
    if (alpha > 0) {
      // White pixel (masked region — area to edit)
      dst[i] = 255;     // R
      dst[i + 1] = 255; // G
      dst[i + 2] = 255; // B
      dst[i + 3] = 255; // A
    } else {
      // Black pixel (unmasked region — area to keep)
      dst[i] = 0;       // R
      dst[i + 1] = 0;   // G
      dst[i + 2] = 0;   // B
      dst[i + 3] = 255; // A (fully opaque black)
    }
  }

  ctx.putImageData(maskData, 0, 0);

  // Export as PNG blob and convert to base64
  const blob = await new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      (b) => (b ? resolve(b) : reject(new Error('Failed to create mask PNG blob'))),
      'image/png'
    );
  });

  const arrayBuffer = await blob.arrayBuffer();
  const bytes = new Uint8Array(arrayBuffer);
  let binary = '';
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary);
}

/**
 * Convert a base64-encoded binary PNG mask back to colored canvas ImageData.
 * This is the reverse of exportMaskAsPng — white pixels in the binary mask
 * become the specified layer color with full alpha, black pixels become transparent.
 */
export async function importMaskFromBase64(
  base64: string,
  color: string
): Promise<ImageData> {
  const dataUrl = `data:image/png;base64,${base64}`;
  const img = await loadImage(dataUrl);

  const canvas = document.createElement('canvas');
  canvas.width = img.width;
  canvas.height = img.height;
  const ctx = canvas.getContext('2d')!;
  ctx.drawImage(img, 0, 0);
  const binaryMask = ctx.getImageData(0, 0, img.width, img.height);

  const [r, g, b] = hexToRgb(color);
  const src = binaryMask.data;
  const result = ctx.createImageData(img.width, img.height);
  const dst = result.data;

  for (let i = 0; i < src.length; i += 4) {
    if (src[i] > 128) {
      dst[i] = r;
      dst[i + 1] = g;
      dst[i + 2] = b;
      dst[i + 3] = 255;
    }
    // Black pixels remain transparent (alpha=0, default)
  }

  return result;
}

function hexToRgb(hex: string): [number, number, number] {
  const h = hex.replace('#', '');
  return [
    parseInt(h.substring(0, 2), 16),
    parseInt(h.substring(2, 4), 16),
    parseInt(h.substring(4, 6), 16),
  ];
}

/**
 * Client-side post-processing compositing:
 * Overlays the Gemini result ONLY in masked regions onto the original image.
 *
 * For each pixel: if ANY mask layer has alpha > 0 at that pixel,
 * use the result pixel; otherwise keep the original pixel.
 *
 * @param originalUrl  URL of the original image (capture/parent generation)
 * @param resultUrl    URL of the AI-generated result image
 * @param maskLayers   Array of mask layers with their ImageData
 * @returns            Data URL of the composited result
 */
export async function compositeResult(
  originalUrl: string,
  resultUrl: string,
  maskLayers: { imageData: ImageData }[]
): Promise<string> {
  // Load both images
  const [originalImg, resultImg] = await Promise.all([
    loadImage(originalUrl),
    loadImage(resultUrl),
  ]);

  const width = originalImg.width;
  const height = originalImg.height;

  // Draw original image to get its pixel data
  const originalCanvas = document.createElement('canvas');
  originalCanvas.width = width;
  originalCanvas.height = height;
  const originalCtx = originalCanvas.getContext('2d')!;
  originalCtx.drawImage(originalImg, 0, 0);
  const originalData = originalCtx.getImageData(0, 0, width, height);

  // Draw result image to get its pixel data
  const resultCanvas = document.createElement('canvas');
  resultCanvas.width = width;
  resultCanvas.height = height;
  const resultCtx = resultCanvas.getContext('2d')!;
  resultCtx.drawImage(resultImg, 0, 0, width, height);
  const resultData = resultCtx.getImageData(0, 0, width, height);

  // Build a combined mask: for each pixel, check if ANY layer has alpha > 0
  const combinedMask = new Uint8Array(width * height);
  for (const layer of maskLayers) {
    if (!layer.imageData) continue;
    const layerData = layer.imageData.data;
    // Ensure mask dimensions match (they should since all operate on source resolution)
    const layerPixels = Math.min(layer.imageData.width * layer.imageData.height, width * height);
    for (let i = 0; i < layerPixels; i++) {
      if (layerData[i * 4 + 3] > 0) {
        combinedMask[i] = 1;
      }
    }
  }

  // Composite: masked pixels from result, unmasked from original
  const outputData = originalCtx.createImageData(width, height);
  const srcOrig = originalData.data;
  const srcResult = resultData.data;
  const dst = outputData.data;

  for (let i = 0; i < width * height; i++) {
    const idx = i * 4;
    if (combinedMask[i]) {
      // Masked region — use result
      dst[idx] = srcResult[idx];
      dst[idx + 1] = srcResult[idx + 1];
      dst[idx + 2] = srcResult[idx + 2];
      dst[idx + 3] = srcResult[idx + 3];
    } else {
      // Unmasked region — keep original
      dst[idx] = srcOrig[idx];
      dst[idx + 1] = srcOrig[idx + 1];
      dst[idx + 2] = srcOrig[idx + 2];
      dst[idx + 3] = srcOrig[idx + 3];
    }
  }

  // Output to canvas and return as data URL
  const outputCanvas = document.createElement('canvas');
  outputCanvas.width = width;
  outputCanvas.height = height;
  const outputCtx = outputCanvas.getContext('2d')!;
  outputCtx.putImageData(outputData, 0, 0);

  return outputCanvas.toDataURL('image/png');
}

/**
 * Build a single overlay image: original + all mask layers drawn with their colors.
 * Each mask layer is drawn with:
 *   - Solid outline (100% opacity)
 *   - Semi-transparent fill (65% opacity)
 * Returns base64 PNG string (no prefix).
 */
export async function exportMasksAsOverlay(
  sourceImageUrl: string,
  maskLayers: { imageData: ImageData; color: string }[]
): Promise<string> {
  const sourceImg = await loadImage(sourceImageUrl);
  const { width, height } = sourceImg;

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d')!;

  // Draw the original image as base
  ctx.drawImage(sourceImg, 0, 0);

  // Draw each mask layer on top
  for (const layer of maskLayers) {
    if (!layer.imageData) continue;

    const [r, g, b] = hexToRgb(layer.color);
    const layerData = layer.imageData.data;

    // Create a temporary canvas for this mask's fill (semi-transparent)
    const fillCanvas = document.createElement('canvas');
    fillCanvas.width = width;
    fillCanvas.height = height;
    const fillCtx = fillCanvas.getContext('2d')!;
    const fillImgData = fillCtx.createImageData(width, height);
    const dst = fillImgData.data;

    for (let i = 0; i < layerData.length; i += 4) {
      if (layerData[i + 3] > 0) {
        dst[i] = r;
        dst[i + 1] = g;
        dst[i + 2] = b;
        dst[i + 3] = 166; // ~65% opacity
      }
    }
    fillCtx.putImageData(fillImgData, 0, 0);
    ctx.drawImage(fillCanvas, 0, 0);

    // Draw solid outline (dilated edge, 3px width)
    const outlineCanvas = document.createElement('canvas');
    outlineCanvas.width = width;
    outlineCanvas.height = height;
    const outlineCtx = outlineCanvas.getContext('2d')!;
    const outlineImgData = outlineCtx.createImageData(width, height);
    const oDst = outlineImgData.data;

    const outlineWidth = 3;

    // First pass: find edge pixels (mask adjacent to non-mask)
    const isEdgePixel = new Uint8Array(width * height);
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const idx = (y * width + x) * 4;
        if (layerData[idx + 3] > 0) {
          const hasTransparentNeighbor =
            (x > 0 && layerData[((y * width) + (x - 1)) * 4 + 3] === 0) ||
            (x < width - 1 && layerData[((y * width) + (x + 1)) * 4 + 3] === 0) ||
            (y > 0 && layerData[(((y - 1) * width) + x) * 4 + 3] === 0) ||
            (y < height - 1 && layerData[(((y + 1) * width) + x) * 4 + 3] === 0);
          const isBorder = x === 0 || x === width - 1 || y === 0 || y === height - 1;
          if (hasTransparentNeighbor || isBorder) {
            isEdgePixel[y * width + x] = 1;
          }
        }
      }
    }

    // Second pass: dilate edge pixels by outlineWidth
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        if (layerData[(y * width + x) * 4 + 3] === 0) continue;
        let nearEdge = false;
        for (let dy = -outlineWidth; dy <= outlineWidth && !nearEdge; dy++) {
          for (let dx = -outlineWidth; dx <= outlineWidth && !nearEdge; dx++) {
            const nx = x + dx;
            const ny = y + dy;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
              if (isEdgePixel[ny * width + nx]) {
                nearEdge = true;
              }
            }
          }
        }
        if (nearEdge) {
          const idx = (y * width + x) * 4;
          oDst[idx] = r;
          oDst[idx + 1] = g;
          oDst[idx + 2] = b;
          oDst[idx + 3] = 255;
        }
      }
    }
    outlineCtx.putImageData(outlineImgData, 0, 0);
    ctx.drawImage(outlineCanvas, 0, 0);
  }

  // Export as PNG base64
  const blob = await new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      (b) => (b ? resolve(b) : reject(new Error('Failed to create overlay PNG blob'))),
      'image/png'
    );
  });

  const arrayBuffer = await blob.arrayBuffer();
  const bytes = new Uint8Array(arrayBuffer);
  let binary = '';
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary);
}

/**
 * Helper: load an image from a URL and return an HTMLImageElement.
 */
function loadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`Failed to load image: ${url}`));
    img.src = url;
  });
}
