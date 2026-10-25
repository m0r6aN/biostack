import { describe, expect, it } from 'vitest';
import { BADGE_JS, DEFAULT_DENYLIST, EMBED_HEADERS, deniedHost, iframeSnippet, parseEmbedQuery, verdictEmbedHtml, vsEmbedHtml } from '@/lib/embed';
import type { KnowledgePublicClaim } from '@/lib/types';

const claim = (over: Partial<KnowledgePublicClaim> = {}) => ({
  id: 'bpc-157-tendon-healing', statement: 'BPC-157 heals tendons at 250 mcg', verdict: 'PROVEN IN HUMANS', published: true, status: 'published',
  qualifier: 'In adults with tendinopathy, for pain, under trial conditions.', note: 'Two small trials.', version: 3,
  publishedAt: '2026-10-20T00:00:00Z', tier: 'Moderate', sources: [], changelog: [], population: '', outcome: '', ...over,
}) as unknown as KnowledgePublicClaim;

describe('verdict embed', () => {
  const html = verdictEmbedHtml({ compound: 'BPC-157', slug: 'bpc-157', claim: claim(), size: 'compact', theme: 'light', denied: false });

  it('renders BioStack says + claim + chip + PROVEN qualifier + as-of + version + receipts link', () => {
    expect(html).toContain('BIOSTACK SAYS');
    expect(html).toContain('PROVEN IN HUMANS');
    expect(html).toContain('under trial conditions');
    expect(html).toContain('Evidence as of Oct 20, 2026 · v3');
    expect(html).toContain('href="https://biostack.cc/knowledge/bpc-157/claims/bpc-157-tendon-healing" target="_blank" rel="noopener"');
    expect(html).toContain('<meta name="robots" content="noindex, nofollow">');
  });

  it('never shows doses, tracking params, scripts or cookies', () => {
    expect(html).not.toMatch(/\d\s?(mg|mcg)\b/i);
    expect(html).not.toMatch(/utm_|<script|document\.cookie/i);
    expect(EMBED_HEADERS['Content-Security-Policy']).toContain("default-src 'none'");
    expect(EMBED_HEADERS['X-Robots-Tag']).toContain('noindex');
  });

  it('accepts only size and theme options; nothing about the text is configurable', () => {
    expect(parseEmbedQuery('https://biostack.cc/embed/verdict/x?size=full&theme=dark&verdict=MYTH&text=hi')).toEqual({ size: 'full', theme: 'dark' });
    expect(parseEmbedQuery('https://biostack.cc/embed/verdict/x?size=huge')).toEqual({ size: 'compact', theme: 'light' });
  });

  it('shows NOT YET GRADED for unpublished claims and a plain link for denied hosts', () => {
    expect(verdictEmbedHtml({ compound: 'X', slug: 'x', claim: claim({ published: false, status: 'pending-review' } as never), size: 'compact', theme: 'light', denied: false })).toContain('Not yet graded');
    const denied = verdictEmbedHtml({ compound: 'BPC-157', slug: 'bpc-157', claim: claim(), size: 'compact', theme: 'light', denied: true });
    expect(denied).not.toContain('PROVEN IN HUMANS');
    expect(denied).toContain('See the evidence record on BioStack.');
  });

  it('denylists vendor storefront hosts', () => {
    expect(deniedHost('https://shop.myshopify.com/products/x')).toBe(true);
    expect(deniedHost('https://www.amazon.com/dp/1')).toBe(true);
    expect(deniedHost('https://forum.example.org/t/1', 'peptideshop.io')).toBe(false);
    expect(deniedHost('https://peptideshop.io/x', 'peptideshop.io')).toBe(true);
    expect(deniedHost(null)).toBe(false);
    expect(DEFAULT_DENYLIST.length).toBeGreaterThan(3);
  });
});

describe('vs embed, snippet and badge', () => {
  it('renders both chips and the templated ruling', () => {
    const html = vsEmbedHtml({ name: 'BPC-157', verdict: 'ANIMAL ONLY' }, { name: 'TB-500', verdict: 'THIN' }, 'More human data: neither.', '/vs/bpc-157/tb-500', 'Oct 20, 2026', 'dark', false);
    expect(html).toContain('ANIMAL ONLY');
    expect(html).toContain('More human data: neither.');
    expect(html).toContain('https://biostack.cc/vs/bpc-157/tb-500');
  });

  it('offers the spec snippet without utm params', () => {
    const s = iframeSnippet('bpc-157-tendon-healing', 'BPC-157 for tendon healing');
    expect(s).toBe('<iframe src="https://biostack.cc/embed/verdict/bpc-157-tendon-healing" width="360" height="140" title="BioStack says: BPC-157 for tendon healing" loading="lazy" style="border:0"></iframe>');
  });

  it('badge script only builds biostack iframes and validates ids', () => {
    expect(BADGE_JS).toContain('/embed/verdict/');
    expect(BADGE_JS).not.toMatch(/cookie|utm_|fetch\(|XMLHttpRequest/);
    document.body.innerHTML = '<div data-biostack-claim="bpc-157-tendon-healing" data-theme="dark"></div><div data-biostack-claim="<bad>"></div>';
    new Function(BADGE_JS)();
    const frames = document.querySelectorAll('iframe');
    expect(frames).toHaveLength(1);
    expect(frames[0].getAttribute('src')).toBe('https://biostack.cc/embed/verdict/bpc-157-tendon-healing?theme=dark');
  });
});
