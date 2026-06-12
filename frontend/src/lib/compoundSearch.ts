export function getCompoundSearchSuggestions(query: string, compounds: string[], limit = 5): string[] {
  const normalized = query.trim().toLowerCase();
  if (normalized.length < 2) {
    return [];
  }

  return compounds
    .filter((name) => name.toLowerCase().includes(normalized))
    .slice(0, limit);
}

export function resolveCompoundSearchCommit(value: string, compounds: string[]): string {
  const normalized = value.trim();
  if (!normalized) {
    return '';
  }

  return compounds.find((name) => name.toLowerCase() === normalized.toLowerCase()) ?? normalized;
}
