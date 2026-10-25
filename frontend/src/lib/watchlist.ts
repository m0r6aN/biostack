// Growth Phase 6: watchlist + alert preferences. Same-origin calls (the Next rewrite proxies
// /api/v1 to the backend with the session cookie).
export type WatchTargetType = 'compound' | 'claim' | 'vs';
export interface Watch { id: string; targetType: WatchTargetType; targetId: string; createdAtUtc: string }
export type AlertFrequency = 'instant' | 'weekly' | 'off';
export interface AlertPreferences { frequency: AlertFrequency; emailEnabled: boolean }
export interface AlertNotice {
  id: string; alertType: 'verdict-change' | 'study-added'; compoundSlug: string; claimId: string;
  version: number; oldVerdict: string; newVerdict: string; reason: string; createdAtUtc: string;
}

async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(path, { ...init, credentials: 'include', headers: { 'Content-Type': 'application/json', ...(init.headers ?? {}) } });
  if (!res.ok) throw new Error(`watchlist ${res.status}`);
  return (res.status === 204 ? undefined : await res.json()) as T;
}

export const watchlistApi = {
  list: () => call<Watch[]>('/api/v1/watchlist'),
  add: (targetType: WatchTargetType, targetId: string) =>
    call<Watch>('/api/v1/watchlist', { method: 'POST', body: JSON.stringify({ targetType, targetId }) }),
  remove: (id: string) => call<void>(`/api/v1/watchlist/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  preferences: () => call<AlertPreferences>('/api/v1/alerts/preferences'),
  savePreferences: (p: AlertPreferences) => call<AlertPreferences>('/api/v1/alerts/preferences', { method: 'PUT', body: JSON.stringify(p) }),
  notices: () => call<AlertNotice[]>('/api/v1/alerts/notices'),
  unsubscribe: (token: string) => call<void>(`/api/v1/alerts/unsubscribe/${encodeURIComponent(token)}`, { method: 'POST' }),
};

export function watchTargetPath(type: WatchTargetType, id: string): string {
  if (type === 'compound') return `/knowledge/${id}`;
  if (type === 'vs') return `/vs/${id}`;
  return `/c/${id}`;
}
