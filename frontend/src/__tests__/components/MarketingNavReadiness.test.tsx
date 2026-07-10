import { MarketingNav } from '@/components/marketing/MarketingNav';
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
  usePathname: () => '/',
}));

vi.mock('@/components/ui/BioStackLogo', () => ({
  BioStackLogo: () => <span>BioStack</span>,
}));

describe('MarketingNav readiness CTAs', () => {
  it('surfaces public evidence and uses a clear free analyzer CTA', () => {
    render(<MarketingNav />);

    expect(screen.getByRole('link', { name: 'Compounds & Evidence' })).toHaveAttribute(
      'href',
      '/knowledge'
    );
    expect(screen.getByRole('link', { name: 'Analyze My Stack' })).toHaveAttribute(
      'href',
      '/tools/analyzer'
    );
    expect(screen.getByRole('link', { name: 'Start Free' })).toHaveAttribute('href', '/start');
    expect(screen.queryByRole('link', { name: 'Map Stack' })).not.toBeInTheDocument();
  });
});
