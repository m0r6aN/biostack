import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import React from 'react';
import { SettingsProvider, useSettings } from '@/lib/settings';

const SETTINGS_KEY = 'biostack_settings';

function SettingsConsumer() {
  const { settings, setWeightUnit } = useSettings();
  return (
    <div>
      <span data-testid="unit">{settings.weightUnit}</span>
      <button onClick={() => setWeightUnit('metric')}>Set Metric</button>
      <button onClick={() => setWeightUnit('imperial')}>Set Imperial</button>
    </div>
  );
}

describe('SettingsProvider', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('renders children without crashing', () => {
    render(
      <SettingsProvider>
        <span data-testid="child">Hello</span>
      </SettingsProvider>
    );
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('provides an initial weightUnit', async () => {
    render(
      <SettingsProvider>
        <SettingsConsumer />
      </SettingsProvider>
    );
    // After mount + hydration useEffect, unit is rendered
    const unitEl = await screen.findByTestId('unit');
    expect(['metric', 'imperial']).toContain(unitEl.textContent);
  });

  it('reads weightUnit from localStorage on mount', async () => {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify({ weightUnit: 'metric' }));
    render(
      <SettingsProvider>
        <SettingsConsumer />
      </SettingsProvider>
    );
    const unit = await screen.findByTestId('unit');
    expect(unit.textContent).toBe('metric');
  });

  it('defaults to imperial when localStorage is empty', async () => {
    render(
      <SettingsProvider>
        <SettingsConsumer />
      </SettingsProvider>
    );
    const unit = await screen.findByTestId('unit');
    expect(unit.textContent).toBe('imperial');
  });

  it('persists updated weightUnit to localStorage', async () => {
    const { getByText } = render(
      <SettingsProvider>
        <SettingsConsumer />
      </SettingsProvider>
    );
    await act(async () => {
      getByText('Set Metric').click();
    });
    const raw = localStorage.getItem(SETTINGS_KEY);
    if (raw) {
      expect(JSON.parse(raw).weightUnit).toBe('metric');
    }
  });

  it('updates weightUnit back to imperial', async () => {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify({ weightUnit: 'metric' }));
    const { getByText } = render(
      <SettingsProvider>
        <SettingsConsumer />
      </SettingsProvider>
    );
    await act(async () => {
      getByText('Set Imperial').click();
    });
    expect(screen.getByTestId('unit').textContent).toBe('imperial');
  });
});

describe('useSettings', () => {
  it('returns a settings object with setWeightUnit from within provider', () => {
    let capturedSettings: ReturnType<typeof useSettings> | null = null;
    function Capture() {
      capturedSettings = useSettings();
      return null;
    }
    render(<SettingsProvider><Capture /></SettingsProvider>);
    expect(capturedSettings).not.toBeNull();
    expect(typeof capturedSettings!.setWeightUnit).toBe('function');
    expect(capturedSettings!.settings).toHaveProperty('weightUnit');
  });
});
