export const ONBOARDING_PREVIEW_STORAGE_KEY = 'biostack_onboarding_preview';

export type OnboardingPreview = {
  compounds: string[];
  goals: string[];
};

export function emptyOnboardingPreview(): OnboardingPreview {
  return { compounds: [], goals: [] };
}

export function readOnboardingPreview(): OnboardingPreview {
  if (typeof window === 'undefined') {
    return emptyOnboardingPreview();
  }

  try {
    const stored = JSON.parse(
      window.localStorage.getItem(ONBOARDING_PREVIEW_STORAGE_KEY) || '{}'
    ) as Partial<OnboardingPreview>;

    return {
      compounds: Array.isArray(stored.compounds) ? stored.compounds.filter(Boolean) : [],
      goals: Array.isArray(stored.goals) ? stored.goals.filter(Boolean) : [],
    };
  } catch {
    window.localStorage.removeItem(ONBOARDING_PREVIEW_STORAGE_KEY);
    return emptyOnboardingPreview();
  }
}

export function writeOnboardingPreview(preview: OnboardingPreview) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(ONBOARDING_PREVIEW_STORAGE_KEY, JSON.stringify(preview));
}

export function clearOnboardingPreview() {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.removeItem(ONBOARDING_PREVIEW_STORAGE_KEY);
}
