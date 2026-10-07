'use client';

/**
 * Persisted preference for the desktop (lg+) sidebar collapse state. Separate
 * from `isSidebarOpen` in `context.tsx`, which drives the mobile drawer.
 */
export const SIDEBAR_COLLAPSED_KEY = 'biostack.sidebarCollapsed';

let memoryPreference = false;
let persistenceFailed = false;

const listeners = new Set<() => void>();

function notifyListeners(): void {
  for (const listener of listeners) {
    listener();
  }
}

/**
 * Same-tab change notification for the collapse key (the browser `storage`
 * event only fires in *other* tabs). Returns an unsubscribe function.
 */
export function subscribeToSidebarCollapsed(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

/** Reads the persisted collapse preference. Uses the current session preference when storage is unavailable. */
export function readSidebarCollapsed(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  if (persistenceFailed) return memoryPreference;
  try {
    memoryPreference = window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1';
    return memoryPreference;
  } catch {
    return memoryPreference;
  }
}

/** Persists the collapse preference and notifies same-tab subscribers. */
export function writeSidebarCollapsed(collapsed: boolean): void {
  if (typeof window === 'undefined') {
    return;
  }

  memoryPreference = collapsed;
  try {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, collapsed ? '1' : '0');
    persistenceFailed = false;
  } catch {
    // Keep a shared session preference even when persistence is denied.
    persistenceFailed = true;
  }

  notifyListeners();
}

/** A real cross-tab change supersedes any temporary same-tab fallback. */
export function receiveSidebarStorageChange(event: StorageEvent): void {
  if (event.key !== null && event.key !== SIDEBAR_COLLAPSED_KEY) return;
  persistenceFailed = false;
  notifyListeners();
}
