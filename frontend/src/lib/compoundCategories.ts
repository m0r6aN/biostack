// The add-compound form's categories are the domain CompoundCategory values;
// the knowledge base classifies entries with its own vocabulary. This maps
// between them so the goal and name suggestions filter to relevant entries
// instead of matching nothing (e.g. no entry is classified literally
// "Nutraceutical" or "Coenzyme").
export const CATEGORY_CLASSIFICATIONS: Record<string, string[]> = {
  Peptide: ['Peptide', 'Research Compound'],
  Supplement: ['Supplement', 'Amino Acid'],
  Pharmaceutical: ['Pharmaceutical', 'Small Molecule', 'Hormone', 'SARM', 'SERM'],
  Nutraceutical: ['Vitamin', 'Amino Acid', 'Small Molecule', 'Supplement'],
  Coenzyme: ['Small Molecule', 'Vitamin'],
  Other: ['Other', 'Research Compound', 'Hormone', 'SARM', 'SERM', 'Small Molecule'],
};

export function classificationsForCategory(category: string): string[] {
  return CATEGORY_CLASSIFICATIONS[category] ?? [category];
}
