import { render, screen, fireEvent } from '@testing-library/react';
import { FilterBar } from '@/components/research/FilterBar';
import type { ResearchReviewCategory } from '@/lib/research/types';
import { vi } from 'vitest';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/admin/research/compounds',
}));

const categories: ResearchReviewCategory[] = [
  { name: 'Safety Critical', count: 2, compounds: [], signals: [], recommendedActions: [] },
  { name: 'Regulatory / Approval', count: 1, compounds: [], signals: [], recommendedActions: [] },
];

describe('FilterBar', () => {
  it('renders readiness chips with counts', () => {
    render(<FilterBar blockedCount={3} reviewCount={7} candidateCount={4} categories={categories} />);
    expect(screen.getByText('Blocked (3)')).toBeInTheDocument();
    expect(screen.getByText('Review Required (7)')).toBeInTheDocument();
    expect(screen.getByText('Candidate (4)')).toBeInTheDocument();
  });

  it('renders review category chips', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    expect(screen.getByText('Safety Critical')).toBeInTheDocument();
    expect(screen.getByText('Regulatory / Approval')).toBeInTheDocument();
  });

  it('"More filters" expander is collapsed by default — Strong tier hidden', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    expect(screen.queryByText('Strong')).not.toBeInTheDocument();
  });

  it('clicking "More filters" reveals evidence tier chips', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    fireEvent.click(screen.getByText('More filters'));
    expect(screen.getByText('Strong')).toBeInTheDocument();
  });
});
