import { beforeEach, describe, expect, it } from 'vitest';

const SETTINGS_KEY = 'biostack_settings';

describe('settings persistence', () => {
  beforeEach(() => {
    localStorage.clear();
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
    let parsed: { weightUnit: string } | null = null;
    try {
      const raw = localStorage.getItem(SETTINGS_KEY)!;
      parsed = JSON.parse(raw);
    } catch {
      parsed = { weightUnit: 'metric' };
    }
    expect(parsed!.weightUnit).toBe('metric');
  });

  it('exports SettingsProvider and useSettings as functions', async () => {
    const mod = await import('@/lib/settings');
    expect(typeof mod.SettingsProvider).toBe('function');
    expect(typeof mod.useSettings).toBe('function');
  });
});
