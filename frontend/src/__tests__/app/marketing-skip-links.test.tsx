import FaqPage from '@/app/faq/page';
import HowItWorksPage from '@/app/how-it-works/page';
import PricingPage from '@/app/pricing/page';
import ProvidersPage from '@/app/providers/page';
import SafetyPage from '@/app/safety/page';
import { cleanup, render, screen } from '@testing-library/react';
import type { ComponentProps, ComponentType } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/',
}));

vi.mock('@/components/marketing/MarketingNav', () => ({
  MarketingNav: () => <nav aria-label="Marketing">Marketing nav</nav>,
}));

vi.mock('@/components/marketing/MarketingFooter', () => ({
  MarketingFooter: () => <footer>Marketing footer</footer>,
}));

vi.mock('@/components/marketing/ProviderAccessForm', () => ({
  ProviderAccessForm: () => <div>Provider access form</div>,
}));

vi.mock('@/components/marketing/IntelligenceProofSection', () => ({
  IntelligenceProofSection: () => <section>Intelligence proof</section>,
}));

// The root layout renders `<a href="#main">Skip to main content</a>` above
// every page, so each public marketing page must expose exactly one main
// landmark that is (a) addressable as `#main` and (b) programmatically
// focusable so activating the skip link actually moves focus.
const SKIP_LINK_TARGET_ID = 'main';

const marketingPages: Array<[route: string, Page: ComponentType]> = [
  ['/pricing', PricingPage],
  ['/how-it-works', HowItWorksPage],
  ['/providers', ProvidersPage],
  ['/faq', FaqPage],
  ['/safety', SafetyPage],
];

describe('public marketing pages expose the skip-link target', () => {
  afterEach(() => {
    cleanup();
  });

  it.each(marketingPages)('%s renders exactly one main landmark with id="main"', (_route, Page) => {
    const { container } = render(<Page />);

    const mains = screen.getAllByRole('main');
    expect(mains).toHaveLength(1);

    const [main] = mains;
    expect(main.tagName).toBe('MAIN');
    expect(main).toHaveAttribute('id', SKIP_LINK_TARGET_ID);
    expect(container.querySelectorAll(`#${SKIP_LINK_TARGET_ID}`)).toHaveLength(1);
    expect(container.querySelector(`#${SKIP_LINK_TARGET_ID}`)).toBe(main);
  });

  it.each(marketingPages)('%s main landmark is programmatically focusable (tabindex="-1")', (_route, Page) => {
    render(<Page />);

    const main = screen.getByRole('main');
    expect(main).toHaveAttribute('tabindex', '-1');

    main.focus();
    expect(document.activeElement).toBe(main);
  });
});
