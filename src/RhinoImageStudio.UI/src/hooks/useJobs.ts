import { useState, useCallback, useRef, useEffect } from 'react';
import { Job } from '@/lib/types';
import { api } from '@/lib/api';

type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

const MAX_RECONNECT_ATTEMPTS = 10;
const BASE_RECONNECT_DELAY = 1000; // 1 second
const MAX_RECONNECT_DELAY = 30000; // 30 seconds

export function useJobs() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
  const eventSourceRef = useRef<EventSource | null>(null);
  const currentProjectIdRef = useRef<string | null>(null);
  const reconnectAttemptRef = useRef(0);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleJobUpdate = useCallback((event: MessageEvent) => {
    try {
      const updatedJob = JSON.parse(event.data) as Job;
      setJobs(current => {
        const idx = current.findIndex(j => j.id === updatedJob.id);
        if (idx >= 0) {
          const newJobs = [...current];
          newJobs[idx] = updatedJob;
          return newJobs;
        }
        return [updatedJob, ...current];
      });
    } catch (e) {
      console.error('Failed to parse job event:', e);
    }
  }, []);

  // Load existing jobs from the API
  const loadExistingJobs = useCallback(async (projectId: string) => {
    try {
      const existingJobs = await api.jobs.list(projectId);
      setJobs(existingJobs);
    } catch (error) {
      console.error('Failed to load existing jobs:', error);
    }
  }, []);

  const connect = useCallback((projectId: string) => {
    // Close existing connection
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }

    // Clear any pending reconnect
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
      reconnectTimeoutRef.current = null;
    }

    setConnectionStatus('connecting');
    currentProjectIdRef.current = projectId;

    const eventSource = new EventSource(`/api/projects/${projectId}/events`);

    eventSource.onopen = () => {
      setConnectionStatus('connected');
      reconnectAttemptRef.current = 0; // Reset reconnect attempts on successful connection
    };

    eventSource.onmessage = handleJobUpdate;

    eventSource.onerror = () => {
      eventSource.close();
      eventSourceRef.current = null;

      // Don't reconnect if we've intentionally unsubscribed
      if (!currentProjectIdRef.current) {
        setConnectionStatus('disconnected');
        return;
      }

      // Attempt to reconnect with exponential backoff
      if (reconnectAttemptRef.current < MAX_RECONNECT_ATTEMPTS) {
        setConnectionStatus('reconnecting');
        const delay = Math.min(
          BASE_RECONNECT_DELAY * Math.pow(2, reconnectAttemptRef.current),
          MAX_RECONNECT_DELAY
        );

        console.log(`SSE disconnected. Reconnecting in ${delay}ms (attempt ${reconnectAttemptRef.current + 1}/${MAX_RECONNECT_ATTEMPTS})`);

        reconnectTimeoutRef.current = setTimeout(() => {
          reconnectAttemptRef.current++;
          if (currentProjectIdRef.current) {
            connect(currentProjectIdRef.current);
          }
        }, delay);
      } else {
        console.error('Max reconnection attempts reached. SSE connection failed.');
        setConnectionStatus('disconnected');
      }
    };

    eventSourceRef.current = eventSource;
  }, [handleJobUpdate]);

  const subscribe = useCallback(async (projectId: string) => {
    // Don't reconnect if already connected to same project
    if (currentProjectIdRef.current === projectId && eventSourceRef.current) {
      return;
    }

    // Load existing jobs first
    await loadExistingJobs(projectId);

    // Connect to SSE
    connect(projectId);
  }, [connect, loadExistingJobs]);

  const unsubscribe = useCallback(() => {
    // Clear pending reconnect
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
      reconnectTimeoutRef.current = null;
    }

    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }

    currentProjectIdRef.current = null;
    reconnectAttemptRef.current = 0;
    setConnectionStatus('disconnected');
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
      }
    };
  }, []);

  return { jobs, subscribe, unsubscribe, connectionStatus };
}
