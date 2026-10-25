import { isPublished } from '@/lib/claimVerdicts';
import { formatAsOf } from '@/lib/claimVerdicts';
import { lintCopy } from '@/lib/copyLint';
import { redactDoseText } from '@/lib/doseRedaction';
import { SITE_ORIGIN } from '@/lib/site';
import type { KnowledgePublicClaim } from '@/lib/types';

// Growth Phase 8 (03 §8): "BioStack says" embeds. Plain server HTML (no app shell, no scripts,
// no cookies, no tracking, no utm params: owner ruling 2026-10-24). Always the latest published
// version; nothing about the text is configurable; doses, vendors and community themes never render.

export type EmbedSize = 'compact' | 'full';
export type EmbedTheme = 'light' | 'dark';
export const EMBED_SIZES = { compact: { width: 360, height: 140 }, full: { width: 360, height: 260 }, vs: { width: 360, height: 220 } } as const;

const CHIP: Record<string, string> = {
  'PROVEN IN HUMANS': '#047857', PROMISING: '#0369A1', 'ANIMAL ONLY': '#B45309', THIN: '#4B5563', MYTH: '#B91C1C', 'NOT YET GRADED': '#6B7280',
};

/** Vendor storefronts that frame embeds commercially (06 §E5). Extend via EMBED_HOST_DENYLIST (comma-separated). */
export const DEFAULT_DENYLIST = ['amazon.com', 'iherb.com', 'vitacost.com', 'ebay.com', 'etsy.com', 'aliexpress.com', 'shopify.com', 'myshopify.com', 'walmart.com'];

export function deniedHost(referer: string | null, extra: string = process.env.EMBED_HOST_DENYLIST ?? ''): boolean {
  if (!referer) return false;
  let host: string;
  try { host = new URL(referer).hostname.toLowerCase(); } catch { return false; }
  const list = [...DEFAULT_DENYLIST, ...extra.split(',').map(s => s.trim().toLowerCase()).filter(Boolean)];
  return list.some(d => host === d || host.endsWith(`.${d}`));
}

export function parseEmbedQuery(url: string): { size: EmbedSize; theme: EmbedTheme } {
  const q = new URL(url).searchParams;
  return { size: q.get('size') === 'full' ? 'full' : 'compact', theme: q.get('theme') === 'dark' ? 'dark' : 'light' };
}

export const esc = (s: string) => s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));

export const EMBED_HEADERS: Record<string, string> = {
  'Content-Type': 'text/html; charset=utf-8',
  'X-Robots-Tag': 'noindex, nofollow',
  'Cache-Control': 'public, max-age=300, s-maxage=600, stale-while-revalidate=3600',
  // Embeddable anywhere; nothing inside may run script or load third-party resources.
  'Content-Security-Policy': "default-src 'none'; style-src 'unsafe-inline'; img-src 'self'; frame-ancestors *; base-uri 'none'; form-action 'none'",
  'Referrer-Policy': 'no-referrer',
};

function page(body: string, theme: EmbedTheme, height: number): string {
  const dark = theme === 'dark';
  return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="robots" content="noindex, nofollow">`
    + `<meta name="viewport" content="width=device-width, initial-scale=1"><title>BioStack says</title><style>`
    + `*{box-sizing:border-box}body{margin:0;font:14px/1.4 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;`
    + `background:${dark ? '#0B1117' : '#FFFFFF'};color:${dark ? '#F3F4F6' : '#111827'}}`
    + `.b{height:${height}px;padding:12px 14px;border:1px solid ${dark ? '#1F2937' : '#E5E7EB'};border-radius:10px;display:flex;flex-direction:column;gap:6px;overflow:hidden}`
    + `.k{font-size:11px;font-weight:700;letter-spacing:.08em;color:${dark ? '#6EE7B7' : '#047857'}}`
    + `.c{font-weight:600;overflow:hidden;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical}`
    + `.chip{display:inline-block;color:#fff;font-size:11px;font-weight:700;padding:2px 6px;border-radius:4px}`
    + `.q,.m{font-size:12px;color:${dark ? '#9CA3AF' : '#4B5563'}}a{color:${dark ? '#67E8F9' : '#0E7490'}}`
    + `.row{display:flex;gap:10px;justify-content:space-between}.f{margin-top:auto;display:flex;justify-content:space-between;font-size:12px}`
    + `</style></head><body>${body}</body></html>`;
}

const chip = (verdict: string) => `<span class="chip" style="background:${CHIP[verdict] ?? CHIP['NOT YET GRADED']}">${esc(verdict)}</span>`;
const receipts = (path: string) => `<a href="${esc(`${SITE_ORIGIN}${path}`)}" target="_blank" rel="noopener">Receipts →</a>`;

/** Only text that passes the copy lint and has no dose token reaches an embed. */
function safe(text: string): string {
  const clean = redactDoseText(text);
  return lintCopy(clean).some(h => h.rule.startsWith('3a') || h.rule.startsWith('3c')) ? '' : clean;
}

export interface VerdictEmbedInput { compound: string; slug: string; claim: KnowledgePublicClaim | null; size: EmbedSize; theme: EmbedTheme; denied: boolean }

export function verdictEmbedHtml({ compound, slug, claim, size, theme, denied }: VerdictEmbedInput): string {
  const { height } = EMBED_SIZES[size];
  if (!claim || denied) {
    const path = claim ? `/knowledge/${slug}/claims/${claim.id}` : '/knowledge';
    return page(`<div class="b"><span class="k">BIOSTACK SAYS</span><span class="c">See the evidence record on BioStack.</span><div class="f">${receipts(path)}</div></div>`, theme, height);
  }
  const published = isPublished(claim);
  const verdict = published ? claim.verdict : 'NOT YET GRADED';
  const qualifier = published && claim.verdict === 'PROVEN IN HUMANS' && claim.qualifier ? `<span class="q">${esc(safe(claim.qualifier))}</span>` : '';
  const note = size === 'full' && published && claim.note ? `<span class="m">${esc(safe(claim.note))}</span>` : '';
  const asOf = published ? formatAsOf(claim.publishedAt ?? claim.verifiedAt) : '';
  const body = `<div class="b" data-version="${claim.version}"><span class="k">BIOSTACK SAYS</span>`
    + `<span class="c">${esc(compound)}: &ldquo;${esc(safe(claim.statement))}&rdquo;</span>`
    + `<span>${chip(verdict)}</span>${qualifier}${note}`
    + `<div class="f"><span class="m">${asOf ? `Evidence as of ${esc(asOf)} · v${claim.version}` : 'Not yet graded'}</span>${receipts(`/knowledge/${slug}/claims/${claim.id}`)}</div></div>`;
  return page(body, theme, height);
}

export interface VsEmbedSide { name: string; verdict: string }
export function vsEmbedHtml(left: VsEmbedSide | null, right: VsEmbedSide | null, ruling: string | null, path: string, asOf: string, theme: EmbedTheme, denied: boolean): string {
  const { height } = EMBED_SIZES.vs;
  if (!left || !right || denied) {
    return page(`<div class="b"><span class="k">BIOSTACK SAYS</span><span class="c">See the evidence ledger on BioStack.</span><div class="f">${receipts(path)}</div></div>`, theme, height);
  }
  const side = (s: VsEmbedSide) => `<div><div class="c">${esc(s.name)}</div>${chip(s.verdict)}</div>`;
  const body = `<div class="b"><span class="k">BIOSTACK SAYS · HEAD TO HEAD</span><div class="row">${side(left)}${side(right)}</div>`
    + (ruling ? `<span class="m">&ldquo;${esc(ruling)}&rdquo;</span>` : '')
    + `<div class="f"><span class="m">${asOf ? `Evidence as of ${esc(asOf)}` : ''}</span>${receipts(path)}</div></div>`;
  return page(body, theme, height);
}

/** Snippet offered in the share sheet (03 §8), without tracking params. */
export function iframeSnippet(claimId: string, title: string, size: EmbedSize = 'compact'): string {
  const { width, height } = EMBED_SIZES[size];
  const src = `${SITE_ORIGIN}/embed/verdict/${encodeURIComponent(claimId)}${size === 'full' ? '?size=full' : ''}`;
  return `<iframe src="${esc(src)}" width="${width}" height="${height}" title="${esc(`BioStack says: ${title}`)}" loading="lazy" style="border:0"></iframe>`;
}

export const BADGE_JS = `(function(){var o=${JSON.stringify(SITE_ORIGIN)};`
  + `var els=document.querySelectorAll('[data-biostack-claim]');`
  + `for(var i=0;i<els.length;i++){var el=els[i];if(el.getAttribute('data-biostack-done'))continue;`
  + `var id=el.getAttribute('data-biostack-claim')||'';if(!/^[A-Za-z0-9._-]{1,200}$/.test(id))continue;`
  + `var full=el.getAttribute('data-size')==='full';var dark=el.getAttribute('data-theme')==='dark';`
  + `var f=document.createElement('iframe');var q=[];if(full)q.push('size=full');if(dark)q.push('theme=dark');`
  + `f.src=o+'/embed/verdict/'+encodeURIComponent(id)+(q.length?'?'+q.join('&'):'');`
  + `f.width='360';f.height=full?'260':'140';f.loading='lazy';f.title='BioStack says';f.style.border='0';`
  + `f.setAttribute('referrerpolicy','strict-origin');el.setAttribute('data-biostack-done','1');el.appendChild(f);}})();`;
