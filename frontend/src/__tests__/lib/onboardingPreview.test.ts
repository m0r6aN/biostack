import { beforeEach, describe, expect, it } from 'vitest';
import {
  ONBOARDING_PREVIEW_STORAGE_KEY,
  emptyOnboardingPreview,
  readOnboardingPreview,
  writeOnboardingPreview,
  clearOnboardingPreview,
} from '@/lib/onboardingPreview';

describe('onboardingPreview', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  describe('emptyOnboardingPreview', () => {
    it('returns an object with empty arrays', () => {
      const preview = emptyOnboardingPreview();
      expect(preview.compounds).toEqual([]);
      expect(preview.goals).toEqual([]);
    });
  });

  describe('readOnboardingPreview', () => {
    it('returns empty preview when nothing is stored', () => {
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual([]);
      expect(preview.goals).toEqual([]);
    });

    it('reads back what was written', () => {
      window.localStorage.setItem(
        ONBOARDING_PREVIEW_STORAGE_KEY,
        JSON.stringify({ compounds: ['BPC-157', 'TB-500'], goals: ['recovery'] })
      );
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual(['BPC-157', 'TB-500']);
      expect(preview.goals).toEqual(['recovery']);
    });

    it('filters out falsy values from arrays', () => {
      window.localStorage.setItem(
        ONBOARDING_PREVIEW_STORAGE_KEY,
        JSON.stringify({ compounds: ['BPC-157', '', null, 'TB-500'], goals: [false, 'recovery'] })
      );
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual(['BPC-157', 'TB-500']);
      expect(preview.goals).toEqual(['recovery']);
    });

    it('returns empty arrays when stored fields are not arrays', () => {
      window.localStorage.setItem(
        ONBOARDING_PREVIEW_STORAGE_KEY,
        JSON.stringify({ compounds: 'oops', goals: 123 })
      );
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual([]);
      expect(preview.goals).toEqual([]);
    });

    it('returns empty preview and clears storage when JSON is invalid', () => {
      window.localStorage.setItem(ONBOARDING_PREVIEW_STORAGE_KEY, 'not-valid-json{{{');
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual([]);
      expect(preview.goals).toEqual([]);
      expect(window.localStorage.getItem(ONBOARDING_PREVIEW_STORAGE_KEY)).toBeNull();
    });

    it('handles missing fields by defaulting to empty arrays', () => {
      window.localStorage.setItem(ONBOARDING_PREVIEW_STORAGE_KEY, JSON.stringify({}));
      const preview = readOnboardingPreview();
      expect(preview.compounds).toEqual([]);
      expect(preview.goals).toEqual([]);
    });
  });

  describe('writeOnboardingPreview', () => {
    it('persists the preview to localStorage', () => {
      writeOnboardingPreview({ compounds: ['CJC-1295'], goals: ['longevity'] });
      const raw = window.localStorage.getItem(ONBOARDING_PREVIEW_STORAGE_KEY);
      expect(raw).not.toBeNull();
      const parsed = JSON.parse(raw!);
      expect(parsed.compounds).toEqual(['CJC-1295']);
      expect(parsed.goals).toEqual(['longevity']);
    });

    it('can be round-tripped with readOnboardingPreview', () => {
      const preview = { compounds: ['Semaglutide'], goals: ['weight-loss', 'energy'] };
      writeOnboardingPreview(preview);
      const read = readOnboardingPreview();
      expect(read.compounds).toEqual(preview.compounds);
      expect(read.goals).toEqual(preview.goals);
    });
  });

  describe('clearOnboardingPreview', () => {
    it('removes the preview from localStorage', () => {
      writeOnboardingPreview({ compounds: ['BPC-157'], goals: ['recovery'] });
      clearOnboardingPreview();
      expect(window.localStorage.getItem(ONBOARDING_PREVIEW_STORAGE_KEY)).toBeNull();
    });

    it('does not throw when called with nothing stored', () => {
      expect(() => clearOnboardingPreview()).not.toThrow();
    });
  });
});
