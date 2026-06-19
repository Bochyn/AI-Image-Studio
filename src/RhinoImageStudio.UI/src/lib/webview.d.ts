import type { RhinoBridge } from './types';

declare global {
  interface Window {
    chrome?: {
      webview?: {
        hostObjects?: {
          rhino?: RhinoBridge;
        };
      };
    };
  }
}

export {};
