/**
 * URL slug for a public compound dossier (/knowledge/{slug}). Must stay identical to
 * BioStack.Infrastructure.Knowledge.CompoundSlug.ForDossier on the backend (which resolves
 * slugs) and to the curated records' identity.slug.
 * "NAD+" → "nad", "Lion's mane" → "lions-mane", "Creatine monohydrate" → "creatine-monohydrate".
 */
export function compoundSlug(name: string | null | undefined): string {
  if (!name || !name.trim()) return '';
  return name
    .trim()
    .toLowerCase()
    .replace(/['\u2019]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

export function compoundPath(name: string): `/knowledge/${string}` {
  return `/knowledge/${compoundSlug(name)}`;
}
