export type LandingPath = 'starter' | 'experienced' | 'provider' | 'analyzer';

const landingPathEventNames: Record<LandingPath, string> = {
  starter: 'landing_path_selected_starter',
  experienced: 'landing_path_selected_experienced',
  provider: 'landing_path_selected_provider',
  analyzer: 'landing_path_selected_analyzer',
};

export function trackLandingPathSelection(path: LandingPath) {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(
    new CustomEvent('biostack:landing_path_selected', {
      detail: {
        path,
        eventName: landingPathEventNames[path],
        occurredAt: new Date().toISOString(),
      },
    })
  );
}
