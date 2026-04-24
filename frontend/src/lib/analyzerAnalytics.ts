export type AnalyzerEvent =
  | 'analyzer_viewed'
  | 'analyzer_input_mode_selected'
  | 'analyzer_analysis_started'
  | 'analyzer_extraction_failed'
  | 'analyzer_parse_succeeded'
  | 'analyzer_score_shown'
  | 'analyzer_premium_unlock_shown'
  | 'analyzer_premium_clicked'
  | 'analyzer_save_clicked'
  | 'analyzer_convert_clicked';

function dispatchAnalyzerEvent(event: AnalyzerEvent, detail?: Record<string, unknown>) {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent('biostack:analyzer', {
      detail: { event, occurredAt: new Date().toISOString(), ...detail },
    })
  );
}

export const analyzerAnalytics = {
  viewed: () => dispatchAnalyzerEvent('analyzer_viewed'),
  modeSelected: (mode: string) => dispatchAnalyzerEvent('analyzer_input_mode_selected', { mode }),
  analysisStarted: (mode: string) => dispatchAnalyzerEvent('analyzer_analysis_started', { mode }),
  extractionFailed: (mode: string) => dispatchAnalyzerEvent('analyzer_extraction_failed', { mode }),
  parseSucceeded: (score: number) => dispatchAnalyzerEvent('analyzer_parse_succeeded', { score }),
  scoreShown: (score: number) => dispatchAnalyzerEvent('analyzer_score_shown', { score }),
  premiumUnlockShown: () => dispatchAnalyzerEvent('analyzer_premium_unlock_shown'),
  premiumClicked: () => dispatchAnalyzerEvent('analyzer_premium_clicked'),
  saveClicked: () => dispatchAnalyzerEvent('analyzer_save_clicked'),
  convertClicked: () => dispatchAnalyzerEvent('analyzer_convert_clicked'),
};
