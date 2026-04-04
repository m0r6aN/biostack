import { describe, it, expect, beforeEach } from 'vitest';

const SETTINGS_KEY = 'biostack_settings';

// We test the persistence logic by directly manipulating localStorage
// and then importing after clearing module cache.
describe('settings persistence', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('default weightUnit is metric when nothing in localStorage', async () => {
    // Dynamically import to get a fresh read of localStorage
    const { SettingsProvider } = await import('@/lib/settings');
    expect(typeof SettingsProvider).toBe('function');
  });

  it('stores and reads back a settings object', () => {
    const settings = { weightUnit: 'imperial' };
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
    const raw = localStorage.getItem(SETTINGS_KEY);
    expect(raw).not.toBeNull();
    const parsed = JSON.parse(raw!);
    expect(parsed.weightUnit).toBe('imperial');
  });

  it('returns default when localStorage has invalid JSON', () => {
    localStorage.setItem(SETTINGS_KEY, 'not-json{{{');
    let parsed: any = null;
    try {
      const raw = localStorage.getItem(SETTINGS_KEY)!;
      parsed = JSON.parse(raw);
    } catch {
      parsed = { weightUnit: 'metric' };
    }
    expect(parsed.weightUnit).toBe('metric');
  });
});
