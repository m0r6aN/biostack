// Presentation only: original benefit values remain the filter and saved-record keys.
const LONG_TOPIC_LABELS: Record<string, string> = {
  'erectile dysfunction, BPH, or pulmonary arterial hypertension': 'Erectile dysfunction / BPH / PAH',
  'narcolepsy, obstructive sleep apnea sleepiness, or shift work disorder': 'Narcolepsy / OSA sleepiness / shift-work disorder',
};

export function compoundGoalDisplay(value: string): { label: string; context?: string } {
  const research = /^Evidence reviewed for (.+?); BioStack presents this as educational reference, not a recommendation\.$/i.exec(value.trim());
  const topic = research?.[1].replace(/ per (?:product )?label$/i, '');
  let label = topic ? LONG_TOPIC_LABELS[topic] ?? topic : value.trim();
  if (topic) label = label.charAt(0).toUpperCase() + label.slice(1);
  // Keep future, unrecognized prose from widening native select menus as well.
  if (label.length > 56) {
    const prefix = label.slice(0, 55);
    const wordBoundary = prefix.lastIndexOf(' ');
    label = `${prefix.slice(0, wordBoundary > 28 ? wordBoundary : prefix.length)}…`;
  }
  return { label, ...(research || label !== value.trim() ? { context: value } : {}) };
}
