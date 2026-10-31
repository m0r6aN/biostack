import KnowledgePage from '@/app/knowledge/page';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Library search and the full compound list change the page without a navigation. Screen-reader users
// need those changes announced (WCAG 4.1.3) and keyboard focus must not fall to <body> when the
// "Show all compounds" button unmounts (WCAG 2.4.3).

vi.mock('@/components/Header', () => ({ Header: ({ title }: { title: string }) => <header>{title}</header> }));
vi.mock('@/components/growth/LibraryGrowthLists', () => ({ LibraryGrowthLists: () => null }));
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: { id: '1', email: 'a@example.test', displayName: 'A', role: 0 }, loading: false, refresh: vi.fn(), logout: vi.fn() }),
}));
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: { canonicalName: string } }) => <div>{entry.canonicalName}</div>,
}));
vi.mock('@/lib/api', () => ({ apiClient: { getAllKnowledgeCompounds: vi.fn(), checkOverlap: vi.fn() } }));

const entry = (canonicalName: string) => ({
  canonicalName, aliases: [], classification: 'Peptide', evidenceTier: 'Tier1', mechanismSummary: '', sourceReferences: [],
});

// Live regions carry aria-live (not role="status", which the main-ownership test reserves for loading states).
function statusRegions() {
  return [...document.querySelectorAll('p.sr-only[aria-live="polite"]')].map((node) => node.textContent ?? '');
}

describe('KnowledgePage screen-reader announcements', () => {
  beforeEach(() => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockReset().mockResolvedValue([entry('BPC-157'), entry('Semaglutide')] as never);
  });

  it('mounts both status regions up front, empty, so later changes are announced', () => {
    render(<KnowledgePage />);
    expect(statusRegions()).toHaveLength(2);
    expect(statusRegions().filter((text) => text.trim() !== '')).toEqual([]);
  });

  it('announces the number of matches after a search', async () => {
    render(<KnowledgePage />);
    fireEvent.change(screen.getByPlaceholderText(/Search compounds/), { target: { value: 'bpc' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() => expect(statusRegions()).toContain('Search complete: 1 compound matches.'));
  });

  it('announces an empty search with the query', async () => {
    render(<KnowledgePage />);
    fireEvent.change(screen.getByPlaceholderText(/Search compounds/), { target: { value: 'zzzz' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() => expect(statusRegions()).toContain('Search complete: no compounds match zzzz.'));
  });

  it('announces the full list and keeps focus in the section when the button unmounts', async () => {
    render(<KnowledgePage />);
    fireEvent.click(screen.getByRole('button', { name: 'Show all compounds' }));

    expect(screen.getByRole('heading', { name: 'Browse the library' })).toHaveFocus();
    await waitFor(() => expect(statusRegions()).toContain('Compound list loaded: 2 compounds.'));
    expect(screen.getByRole('link', { name: /Semaglutide/ })).toBeInTheDocument();
  });

  it('announces when the list cannot be loaded', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockRejectedValue(new Error('down'));
    render(<KnowledgePage />);
    fireEvent.click(screen.getByRole('button', { name: 'Show all compounds' }));

    await waitFor(() => expect(statusRegions()).toContain('The compound list could not be loaded.'));
  });
});
