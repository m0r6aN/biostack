import ProvidersPage from '@/app/providers/page';
import { render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/providers',
}));

vi.mock('@/components/marketing/MarketingNav', () => ({
  MarketingNav: () => <nav>Marketing nav</nav>,
}));

vi.mock('@/components/marketing/MarketingFooter', () => ({
  MarketingFooter: () => <footer>Footer</footer>,
}));

describe('providers page readiness', () => {
  it('explains provider value, safety boundaries, privacy, and next step', () => {
    render(<ProvidersPage />);

    expect(screen.getByRole('heading', { name: /Client protocol observability/i })).toBeInTheDocument();
    expect(screen.getByText(/What providers can do/i)).toBeInTheDocument();
    expect(screen.getByText(/What BioStack does not do/i)).toBeInTheDocument();
    expect(screen.getByText(/Data ownership and privacy/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Request Provider Pilot Access' })).toHaveAttribute(
      'href',
      '#provider-access-request'
    );
    expect(screen.getByRole('heading', { name: 'Request provider pilot access' })).toBeInTheDocument();
    expect(screen.getByText(/Do not include client, health, compound, or protocol details/i)).toBeInTheDocument();
    expect(screen.getByText('Is BioStack a medical device or EHR?')).toBeInTheDocument();
    expect(screen.getByText('Does BioStack give dosing or treatment recommendations?')).toBeInTheDocument();
    expect(screen.getByText('Can clients revoke access?')).toBeInTheDocument();
  });
});
