'use client';

import { useCallback, useSyncExternalStore } from 'react';
import { readSidebarCollapsed, receiveSidebarStorageChange, subscribeToSidebarCollapsed, writeSidebarCollapsed } from './sidebarCollapse';

function subscribe(onChange: () => void): () => void {
  const unsubscribe = subscribeToSidebarCollapsed(onChange);
  // Each subscription owns its listener; unmounting one consumer must not
  // remove another consumer's cross-tab notification channel.
  const onStorage = (event: StorageEvent) => receiveSidebarStorageChange(event);
  window.addEventListener('storage', onStorage);
  return () => {
    unsubscribe();
    window.removeEventListener('storage', onStorage);
  };
}

function getServerSnapshot(): boolean {
  return false;
}

/**
 * Hydration-safe view of the desktop sidebar collapse preference. The server
 * snapshot is always expanded (false), so first paint never trusts device
 * storage; the client snapshot is re-read whenever this hook (or another
 * consumer/tab) changes it, so the sidebar and any page reacting to the
 * collapse state (e.g. the compounds detail grid) stay in sync.
 */
export function useSidebarCollapsed(): [boolean, (collapsed: boolean) => void] {
  const collapsed = useSyncExternalStore(subscribe, readSidebarCollapsed, getServerSnapshot);
  const setCollapsed = useCallback((next: boolean) => {
    writeSidebarCollapsed(next);
  }, []);
  return [collapsed, setCollapsed];
}
