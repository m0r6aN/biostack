import SupportPage from '@/app/support/page';
import { SUPPORT_EMAIL, SUPPORT_ENTRIES, SUPPORT_GROUPS } from '@/lib/supportContent';
import { cleanup, render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({ usePathname: () => '/support' }));
vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav aria-label="Marketing">Marketing nav</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Marketing footer</footer> }));

const allText = SUPPORT_ENTRIES.flatMap((entry) => [entry.question, ...entry.answer]).join('\n');

describe('/support', () => {
  afterEach(() => cleanup());

  it('covers the topics the product has to explain', () => {
    const questions = SUPPORT_ENTRIES.map((entry) => entry.question.toLowerCase()).join('\n');
    for (const topic of ['signing in', 'consent', 'download my data', 'delete my account', 'evidence tiers', 'report it', 'how much', 'contact biostack']) {
      expect(questions + SUPPORT_GROUPS.map((g) => g.heading.toLowerCase()).join('\n')).toContain(topic);
    }
  });

  it('renders one h1, one heading per group, and one section per question', () => {
    render(<SupportPage />);
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
    for (const group of SUPPORT_GROUPS) {
      expect(screen.getByRole('heading', { level: 2, name: group.heading })).toBeInTheDocument();
    }
    for (const entry of SUPPORT_ENTRIES) {
      expect(screen.getByRole('heading', { level: 3, name: entry.question })).toBeInTheDocument();
    }
  });

  it('offers the contact form and the real support mailbox, and no other address', () => {
    render(<SupportPage />);
    expect(SUPPORT_EMAIL).toBe('support@biostack.cc');
    const mailtos = [...document.querySelectorAll('a[href^="mailto:"]')].map((a) => a.getAttribute('href'));
    expect(mailtos).toEqual([`mailto:${SUPPORT_EMAIL}`]);
    expect(screen.getAllByRole('link', { name: /contact us|open the contact form/i }).some((a) => a.getAttribute('href') === '/contact')).toBe(true);
    const addresses = new Set(allText.match(/[\w.+-]+@[\w-]+(?:\.[\w-]+)+/g) ?? []);
    expect([...addresses]).toEqual([SUPPORT_EMAIL]);
  });

  it('states the no-dose-advice boundary and never offers dosing guidance', () => {
    expect(allText).toMatch(/never tells you what to take or how much/i);
    expect(allText).not.toMatch(/\b\d+(\.\d+)?\s?(mcg|µg|mg|iu|ml|units?)\b/i);
    expect(allText).not.toMatch(/we recommend|you should take|recommended dose/i);
  });

  it('keeps consent, export and deletion facts in line with the product', () => {
    expect(allText).toMatch(/15 minutes/);
    expect(allText).toMatch(/35 days/);
    expect(allText).toMatch(/JSON/);
    expect(allText).toMatch(/CSV/);
    expect(allText).toMatch(/do not sell your data/i);
  });

  it('lists the seven evidence tiers by name', () => {
    for (const tier of ['Established', 'Plausible Theory', 'Early Evidence', 'Anecdotal Signal', 'Contradicted', 'Unsupported', 'Hype Exceeds Evidence']) {
      expect(allText).toContain(tier);
    }
  });

  it('publishes the same questions as FAQPage structured data', () => {
    const { container } = render(<SupportPage />);
    const script = container.querySelector('script[type="application/ld+json"]');
    const data = JSON.parse(script?.textContent ?? '{}');
    expect(data['@type']).toBe('FAQPage');
    expect(data.mainEntity.map((q: { name: string }) => q.name)).toEqual(SUPPORT_ENTRIES.map((e) => e.question));
  });

  it('has unique anchors, and every in-page link resolves', () => {
    render(<SupportPage />);
    const ids = [...SUPPORT_GROUPS.map((g) => g.id), ...SUPPORT_ENTRIES.map((e) => e.id)];
    expect(new Set(ids).size).toBe(ids.length);
    for (const anchor of document.querySelectorAll('a[href^="#"]')) {
      expect(document.getElementById(anchor.getAttribute('href')!.slice(1))).not.toBeNull();
    }
  });

  it('only links to routes that exist in the app', () => {
    const known = ['mailto:support@biostack.cc', '/auth/signin', '/contact', '/onboarding/consent', '/privacy', '/account/data', '/how-it-works', '/knowledge', '/safety', '/knowledge/methodology', '/pricing', '/billing'];
    for (const link of SUPPORT_ENTRIES.flatMap((entry) => entry.links ?? [])) {
      expect(known).toContain(link.href);
    }
  });
});
