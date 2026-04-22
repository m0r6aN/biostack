interface SafetyDisclaimerProps {
  type?: 'educational' | 'calculation' | 'observation' | 'general';
}

export function SafetyDisclaimer({ type = 'general' }: SafetyDisclaimerProps) {
  const disclaimers = {
    educational: 'Observational only — not medical advice. Do not change clinical treatment without your provider.',
    calculation: 'Calculation only — not medical advice. Do not change clinical treatment without your provider.',
    observation: 'Observational only — not medical advice. Do not change clinical treatment without your provider.',
    general: 'Observational only — not medical advice. Do not change clinical treatment without your provider.',
  };

  return (
    <div className="mt-4 p-3 bg-amber-500/10 border border-amber-400/15 rounded-xl">
      <p className="text-xs text-amber-200/70">{disclaimers[type]}</p>
    </div>
  );
}
