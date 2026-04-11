'use client';

import { ReactNode, useContext, useEffect, useState } from 'react';

export type WeightUnit = 'metric' | 'imperial';

export interface Settings {
  weightUnit: WeightUnit;
}

const SETTINGS_KEY = 'biostack_settings';

const defaultSettings: Settings = {
  weightUnit: 'imperial',
};

function loadSettings(): Settings {
  if (typeof window === 'undefined') return defaultSettings;
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    if (!raw) return defaultSettings;
    return { ...defaultSettings, ...JSON.parse(raw) };
  } catch {
    return defaultSettings;
  }
}

function saveSettings(settings: Settings) {
  if (typeof window === 'undefined') return;
  localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
}

// ─── Context ─────────────────────────────────────────────────────────────────

interface SettingsContextType {
  settings: Settings;
  setWeightUnit: (unit: WeightUnit) => void;
}

import React from 'react';

const SettingsContext = React.createContext<SettingsContextType>({
  settings: defaultSettings,
  setWeightUnit: () => {},
});

export function SettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<Settings>(defaultSettings);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setSettings(loadSettings());
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (hydrated) saveSettings(settings);
  }, [settings, hydrated]);

  const setWeightUnit = (unit: WeightUnit) =>
    setSettings(prev => ({ ...prev, weightUnit: unit }));

  return React.createElement(SettingsContext.Provider, { value: { settings, setWeightUnit } }, children);
}

export function useSettings() {
  return useContext(SettingsContext);
}
