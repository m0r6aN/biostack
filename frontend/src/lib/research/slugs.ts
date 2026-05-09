export function toSlug(canonicalName: string): string {
  return canonicalName
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

export function buildSlugMap(
  compounds: ReadonlyArray<{ name: string }>
): Map<string, string> {
  const map = new Map<string, string>();
  for (const c of compounds) map.set(toSlug(c.name), c.name);
  return map;
}
