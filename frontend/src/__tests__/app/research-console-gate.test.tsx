import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ResearchLayout from '@/app/admin/research/layout';

const navigation = vi.hoisted(() => ({ pathname: '/admin/research' }));

vi.mock('next/navigation', () => ({
  usePathname: () => navigation.pathname,
}));

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.ComponentProps<'a'>) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

describe('research console gate', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('explains the local-only console for artifact pages in production', () => {
    vi.stubEnv('NODE_ENV', 'production');
    navigation.pathname = '/admin/research';

    render(
      <ResearchLayout>
        <div>artifact console</div>
      </ResearchLayout>,
    );

    expect(screen.getByText('Local pipeline console')).toBeInTheDocument();
    expect(screen.queryByText('artifact console')).not.toBeInTheDocument();
  });

  it('keeps the database-backed staged-reviews surface live in production', () => {
    vi.stubEnv('NODE_ENV', 'production');
    navigation.pathname = '/admin/research/staged-reviews';

    render(
      <ResearchLayout>
        <div>staged reviews</div>
      </ResearchLayout>,
    );

    expect(screen.getByText('staged reviews')).toBeInTheDocument();
    expect(screen.queryByText('Local pipeline console')).not.toBeInTheDocument();
  });

  it('renders children in development', () => {
    navigation.pathname = '/admin/research';

    render(
      <ResearchLayout>
        <div>artifact console</div>
      </ResearchLayout>,
    );

    expect(screen.getByText('artifact console')).toBeInTheDocument();
  });
});
