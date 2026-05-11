// BioStack Telemetry
// Lightweight event emitter for surface analytics.
// In production: swap the emit() body for your analytics provider.

export type TelemetryEvent =
  | { name: 'surface_view'; surface: string }
  | { name: 'why_drawer_open'; surface: string; metric?: string }
  | { name: 'counterfactual_variant_save'; variantType: string }
  | { name: 'observation_debt_resolve'; itemType: string }
  | { name: 'stack_graph_node_click'; compoundName: string }
  | { name: 'stack_graph_edge_click'; interactionType: string }
  | { name: 'flight_recorder_event_click'; eventType: string }
  | { name: 'operating_state_cta_click'; state: string }
  | { name: 'confidence_profile_view'; surface: string };

const listeners: Array<(event: TelemetryEvent) => void> = [];

export function track(event: TelemetryEvent): void {
  if (process.env.NODE_ENV !== 'production') {
    // eslint-disable-next-line no-console
    console.debug('[telemetry]', event.name, event);
  }
  for (const fn of listeners) {
    try { fn(event); } catch { /* never let telemetry crash the app */ }
  }
}

export function onTrack(fn: (event: TelemetryEvent) => void): () => void {
  listeners.push(fn);
  return () => {
    const idx = listeners.indexOf(fn);
    if (idx !== -1) listeners.splice(idx, 1);
  };
}
