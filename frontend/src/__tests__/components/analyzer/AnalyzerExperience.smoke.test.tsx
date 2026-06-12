import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

// ── Mocks (analyzer test conventions) ─────────────────────────────────────────

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: null, loading: false }),
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    analyzeProtocol: vi.fn(),
    getProfiles: vi.fn().mockResolvedValue([]),
  },
  ApiError: class ApiError extends Error {
    status: number;
    constructor(message: string, status = 500) {
      super(message);
      this.status = status;
    }
  },
}));

import { AnalyzerExperience } from '@/components/tools/analyzer/AnalyzerExperience';

describe('AnalyzerExperience (smoke)', () => {
  it('renders the heading and input-stage mode tabs on initial mount', () => {
    render(<AnalyzerExperience />);

    expect(
      screen.getByRole('heading', { name: /Analyze any protocol, in any format you actually have/i }),
    ).toBeInTheDocument();

    expect(screen.getByRole('button', { name: 'Paste' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Scan' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Link' })).toBeInTheDocument();
  });
});
