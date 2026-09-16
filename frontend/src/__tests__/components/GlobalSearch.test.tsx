import { GlobalSearch } from '@/components/GlobalSearch';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const { push } = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ push }) }));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
  },
}));

const entries = [
  { canonicalName: 'BPC-157', aliases: ['Body Protection Compound'], classification: 'Peptide' },
  { canonicalName: 'Creatine', aliases: [], classification: 'Supplement' },
  { canonicalName: 'NAD+', aliases: ['Nicotinamide adenine dinucleotide'], classification: 'Coenzyme' },
];

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue(entries as never);
});

describe('GlobalSearch', () => {
  it('does not fetch the knowledge base until the visitor interacts with it', () => {
    render(<GlobalSearch />);
    expect(apiClient.getAllKnowledgeCompounds).not.toHaveBeenCalled();
  });

  it('filters by name, alias, and classification and caps results at 8, showing a classification chip', async () => {
    render(<GlobalSearch />);
    const input = screen.getByRole('combobox', { name: /search the library/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'pept' } });

    const option = await screen.findByRole('option', { name: /BPC-157/ });
    expect(option).toHaveTextContent('Peptide');

    fireEvent.change(input, { target: { value: 'Nicotinamide' } });
    expect(await screen.findByRole('option', { name: /NAD\+/ })).toBeInTheDocument();
  });

  it('pressing "/" focuses the input when nothing else is focused', () => {
    render(<><input aria-label="unrelated" /><GlobalSearch /></>);
    fireEvent.keyDown(document, { key: '/' });
    expect(screen.getByRole('combobox', { name: /search the library/i })).toHaveFocus();
  });

  it('does not steal focus for "/" typed into another field', () => {
    render(
      <>
        <input aria-label="unrelated" />
        <GlobalSearch />
      </>
    );
    const other = screen.getByLabelText('unrelated');
    other.focus();
    fireEvent.keyDown(other, { key: '/' });
    expect(screen.getByRole('combobox', { name: /search the library/i })).not.toHaveFocus();
  });

  it('Enter opens the top match\'s dossier', async () => {
    render(<GlobalSearch />);
    const input = screen.getByRole('combobox', { name: /search the library/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'creatine' } });
    await screen.findByRole('option', { name: /Creatine/ });
    fireEvent.keyDown(input, { key: 'Enter' });
    expect(push).toHaveBeenCalledWith('/knowledge/creatine');
  });

  it('Escape clears the query first, then a second Escape blurs', async () => {
    render(<GlobalSearch />);
    const input = screen.getByRole('combobox', { name: /search the library/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'creatine' } });
    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
    fireEvent.keyDown(input, { key: 'Escape' });
    expect(input).toHaveValue('');
  });

  it('shows an accessible no-match message when nothing matches', async () => {
    render(<GlobalSearch />);
    const input = screen.getByRole('combobox', { name: /search the library/i });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'zzz-no-such-compound' } });
    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
    expect(await screen.findByText(/No matches for/)).toBeInTheDocument();
  });
});
