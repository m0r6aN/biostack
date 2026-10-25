/**
 * Source-domain allowlist for outbound links (voice guide 3c, safety 06 §B blocker).
 * Mirrors backend PublicSourcePolicy.AllowedHostSuffixes; the backend test enforces it on
 * the data, and __tests__/lib/outboundLinks.test.ts enforces it on hard-coded links.
 */
export const ALLOWED_SOURCE_HOST_SUFFIXES = [
  'pubmed.ncbi.nlm.nih.gov', 'pmc.ncbi.nlm.nih.gov', 'ncbi.nlm.nih.gov', 'europepmc.org', 'doi.org',
  'clinicaltrials.gov', 'isrctn.com', 'trialsearch.who.int', 'clinicaltrialsregister.eu', 'gsk-studyregister.com',
  'fda.gov', 'nih.gov', 'nlm.nih.gov', 'cdc.gov', 'dea.gov', 'federalregister.gov', 'ncats.io',
  'ema.europa.eu', 'europa.eu', 'who.int', 'canada.ca', 'tga.gov.au', 'gov.uk', 'australianprescriber.tg.org.au',
  'go.drugbank.com', 'wada-ama.org', 'usada.org',
  'nejm.org', 'jamanetwork.com', 'thelancet.com', 'bmj.com', 'nature.com', 'link.springer.com', 'springer.com',
  'academic.oup.com', 'frontiersin.org', 'journals.sagepub.com', 'onlinelibrary.wiley.com', 'sciencedirect.com',
  'cell.com', 'mdpi.com', 'plos.org', 'jstage.jst.go.jp', 'ahajournals.org', 'diabetesjournals.org',
  'acrabstracts.org', 'endocrine.org', 'tandfonline.com', 'karger.com', 'biomedcentral.com',
  'pure.johnshopkins.edu', 'digitalcommons.wustl.edu',
] as const;

export function isAllowlistedSourceUrl(url: string | null | undefined): boolean {
  if (!url) return false;
  let parsed: URL;
  try { parsed = new URL(url); } catch { return false; }
  if (parsed.protocol !== 'https:' && parsed.protocol !== 'http:') return false;
  const host = parsed.hostname.toLowerCase();
  return ALLOWED_SOURCE_HOST_SUFFIXES.some(s => host === s || host.endsWith(`.${s}`));
}
