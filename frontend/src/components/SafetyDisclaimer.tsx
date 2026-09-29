interface SafetyDisclaimerProps {
  type?: 'educational' | 'calculation' | 'observation' | 'general';
}

export function SafetyDisclaimer({ type = 'general' }: SafetyDisclaimerProps) {
  const disclaimers = {
    educational: 'Educational reference only — not medical advice.',
    calculation: 'This is a mathematical calculation only. This is not medical advice.',
    observation: 'This is pathway overlap detection for observational purposes, not clinical interaction checking.',
    general: 'This platform is for observational and educational purposes only.',
  };

  return (
    <div className="mt-4 p-3 bg-amber-500/10 border border-amber-400/15 rounded-xl">
      <p className="text-xs text-amber-200/70">{disclaimers[type]}</p>
    </div>
  );
}
