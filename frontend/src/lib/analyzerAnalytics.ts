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
  | 'analyzer_profile_nudge_clicked'
  | 'analyzer_draft_recovered'
  | 'analyzer_draft_imported'
  | 'analyzer_draft_dismissed'
  | 'analyzer_extraction_failed'
  | 'analyzer_parse_succeeded'
  | 'analyzer_score_shown'
  | 'analyzer_premium_unlock_shown'
  | 'analyzer_premium_clicked';

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

export const analyzerAnalytics = {
  viewed: () => trackAnalyzerEvent('analyzer_viewed'),
  modeSelected: (mode: string) => trackAnalyzerEvent('analyzer_input_mode_selected', { mode }),
  analysisStarted: (mode: string) => trackAnalyzerEvent('analyzer_analysis_started', { mode }),
  extractionFailed: (mode: string) => trackAnalyzerEvent('analyzer_extraction_failed', { mode }),
  parseSucceeded: (score: number) => trackAnalyzerEvent('analyzer_parse_succeeded', { score }),
  scoreShown: (score: number) => trackAnalyzerEvent('analyzer_score_shown', { score }),
  premiumUnlockShown: () => trackAnalyzerEvent('analyzer_premium_unlock_shown'),
  premiumClicked: () => trackAnalyzerEvent('analyzer_premium_clicked'),
  saveClicked: () => trackAnalyzerEvent('analyzer_save_clicked'),
  convertClicked: () => trackAnalyzerEvent('analyzer_convert_clicked'),
};
