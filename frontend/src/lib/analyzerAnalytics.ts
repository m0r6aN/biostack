export type AnalyzerAnalyticsEvent =
  | 'analyzer_viewed'
  | 'analyzer_input_mode_selected'
  | 'analyzer_analysis_started'
  | 'analyzer_result_viewed'
  | 'analyzer_score_visible'
  | 'analyzer_why_section_viewed'
  | 'analyzer_comparison_viewed'
  | 'analyzer_unlock_clicked'
  | 'analyzer_save_clicked'
  | 'analyzer_convert_clicked'
  | 'analyzer_example_loaded'
  | 'analyzer_scan_selected'
  | 'analyzer_goal_selected'
  | 'analyzer_context_opened'
  | 'analyzer_context_prefilled'
  | 'analyzer_profile_nudge_clicked';

export function trackAnalyzerEvent(
  eventName: AnalyzerAnalyticsEvent,
  detail: Record<string, string | number | boolean | null | undefined> = {}
) {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(
    new CustomEvent('biostack:analyzer_event', {
      detail: {
        eventName,
        occurredAt: new Date().toISOString(),
        ...detail,
      },
    })
  );
}
