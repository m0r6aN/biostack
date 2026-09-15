'use client';

/**
 * Persisted preference for the desktop (lg+) sidebar collapse state. Separate
 * from `isSidebarOpen` in `context.tsx`, which drives the mobile drawer.
 */
export const SIDEBAR_COLLAPSED_KEY = 'biostack.sidebarCollapsed';

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

/** Reads the persisted collapse preference. Defaults to expanded (false) when storage is unavailable/unset. */
export function readSidebarCollapsed(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  try {
    return window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1';
  } catch {
    return false;
  }
}

/** Persists the collapse preference and notifies same-tab subscribers. */
export function writeSidebarCollapsed(collapsed: boolean): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, collapsed ? '1' : '0');
  } catch {
    // Storage unavailable (private browsing, quota, disabled) — the collapse
    // still applies for this render, it just won't persist across reloads.
  }

  notifyListeners();
}
