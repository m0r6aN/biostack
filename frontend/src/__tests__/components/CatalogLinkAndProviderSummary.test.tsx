import CompoundsPage from '@/app/compounds/page';
import { ProviderObservationalSummary } from '@/components/protocols/ProviderObservationalSummary';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol, providerSummaryGeneratedAt } from '../fixtures/providerSummary';

const profileState = vi.hoisted(() => ({ currentProfileId: 'profile-fixture' }));
vi.mock('@/lib/context', () => ({ useProfile: () => profileState }));
const searchParamsState = vi.hoisted(() => ({ compound: null as string | null }));
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useSearchParams: () => ({ get: (key: string) => (key === 'compound' ? searchParamsState.compound : null) }),
}));
vi.mock('@/components/Header', () => ({ Header: ({ actions }: { actions?: React.ReactNode }) => <header>{actions}</header> }));
vi.mock('@/components/ActiveProfileChip', () => ({ ActiveProfileChip: () => null }));
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: KnowledgeEntry }) => <div>{entry.canonicalName}</div>,
}));
vi.mock('@/lib/api', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/api')>()),
  apiClient: {
    getCompounds: vi.fn(),
    getAllKnowledgeCompounds: vi.fn(),
    createCompound: vi.fn(),
    getKnowledgeEntry: vi.fn(),
    updateCompound: vi.fn(),
    deleteCompound: vi.fn(),
    checkOverlap: vi.fn(),
  },
}));

// The library knows published dosing text; linking must never copy any of it into the form.
const entry = (name: string, slug: string | undefined, classification: string, aliases: string[] = []): KnowledgeEntry =>
  ({
    canonicalName: name, slug, aliases, classification, benefits: [],
    recommendedDosage: '500 mcg daily', standardDosageRange: '250-500 mcg', maxReportedDose: '1000 mcg', frequency: 'daily',
  }) as unknown as KnowledgeEntry;

const LIBRARY = [
  entry('Creatine', 'creatine', 'Supplement', ['creatine monohydrate']),
  entry('Vitamin D3', 'vitamin-d3', 'Vitamin', ['cholecalciferol']),
  entry('BPC-157', 'bpc-157', 'Peptide'),
];

const DOSE_LABELS = ['Dose amount', 'Dose unit', 'Dose frequency', 'Dose time or schedule'] as const;
const MANUAL = '4. Optional: Manual Search/Entry';

async function openAddForm() {
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));
  await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
}

const submit = () => fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound' }).at(-1)!);
const type = (value: string) => fireEvent.change(screen.getByLabelText(MANUAL), { target: { value } });

beforeEach(() => {
  vi.resetAllMocks();
  profileState.currentProfileId = 'profile-fixture';
  searchParamsState.compound = null;
  window.localStorage.clear();
  vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue(LIBRARY);
  vi.mocked(apiClient.getKnowledgeEntry).mockRejectedValue(new Error('not in library'));
  vi.mocked(apiClient.createCompound).mockImplementation(async (_p, data) => ({ ...data, id: 'created-1' }) as CompoundRecord);
});

describe('add flow: catalog link', () => {
  it('shows library matches by name and alias with their category, and typing alone never links', async () => {
    await openAddForm();
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Supplement' } });

    type('cholecal');
    const list = await screen.findByRole('list', { name: "Matches in BioStack's library" });
    expect(within(list).getByText('Vitamin D3')).toBeInTheDocument();
    expect(within(list).getByText('Vitamin')).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Not linked to BioStack');
    expect(screen.getByRole('status')).toHaveTextContent('Unverified');

    type('Vitamin D3'); // exactly the record's name, still typed: no link
    submit();
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(vi.mocked(apiClient.createCompound).mock.calls[0][1]).not.toHaveProperty('catalogSlug');
  });

  it('links only on an explicit pick, sends just the slug, and never prefills a dose or schedule', async () => {
    await openAddForm();
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Supplement' } });
    type('creat');

    fireEvent.click(await screen.findByRole('button', { name: /Creatine/ }));

    expect(screen.getByLabelText(MANUAL)).toHaveValue('Creatine');
    expect(screen.getByText(/In BioStack: linked to Creatine/)).toBeInTheDocument();
    for (const label of DOSE_LABELS) {
      expect((screen.getByLabelText(label) as HTMLInputElement | HTMLSelectElement).value).toBe('');
    }
    submit();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    const payload = vi.mocked(apiClient.createCompound).mock.calls[0][1];
    expect(payload).toMatchObject({ name: 'Creatine', catalogSlug: 'creatine', doseAmount: null, doseUnit: null, doseFrequency: null, doseTime: null, frequencyKind: null });
    // the form never decides state or category source; the server derives both
    expect(payload).not.toHaveProperty('verificationState');
    expect(payload).not.toHaveProperty('categorySource');
    expect(JSON.stringify(payload)).not.toMatch(/500 mcg|250-500|1000 mcg/);
  });

  it('drops the link when the name is edited away from the record, and on Unlink', async () => {
    await openAddForm();
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Supplement' } });
    type('creat');
    fireEvent.click(await screen.findByRole('button', { name: /Creatine/ }));

    type('Creatine blend');
    expect(screen.queryByText(/In BioStack: linked to/)).not.toBeInTheDocument();
    submit();
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(vi.mocked(apiClient.createCompound).mock.calls[0][1]).not.toHaveProperty('catalogSlug');
  });

  it('unlink button returns the item to Unverified', async () => {
    await openAddForm();
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Supplement' } });
    type('creat');
    fireEvent.click(await screen.findByRole('button', { name: /Creatine/ }));

    fireEvent.click(screen.getByRole('button', { name: 'Unlink' }));

    expect(screen.queryByText(/In BioStack: linked to/)).not.toBeInTheDocument();
    expect(screen.getByText(/Not linked to BioStack/)).toBeInTheDocument();
    submit();
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(vi.mocked(apiClient.createCompound).mock.calls[0][1]).not.toHaveProperty('catalogSlug');
  });

  it('a pick from the compound dropdown links, and a record without a slug never does', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([...LIBRARY, entry('No Slug Peptide', undefined, 'Peptide')]);
    await openAddForm();
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });

    fireEvent.change(screen.getByLabelText('3. Select a Compound'), { target: { value: 'BPC-157' } });
    expect(screen.getByText(/In BioStack: linked to BPC-157/)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('3. Select a Compound'), { target: { value: 'No Slug Peptide' } });
    expect(screen.queryByText(/In BioStack: linked to/)).not.toBeInTheDocument();
  });

  it('a dossier deep link preselects and links the record, with the dose still blank', async () => {
    searchParamsState.compound = 'bpc-157';
    render(<CompoundsPage />);

    await waitFor(() => expect((screen.getByLabelText(MANUAL) as HTMLInputElement).value).toBe('BPC-157'));
    expect(screen.getByText(/In BioStack: linked to BPC-157/)).toBeInTheDocument();
    for (const label of DOSE_LABELS) {
      expect((screen.getByLabelText(label) as HTMLInputElement | HTMLSelectElement).value).toBe('');
    }
  });
});

describe('provider summary: custom items, per-item toggle and other items', () => {
  const live = (id: string, name: string, extra: Partial<CompoundRecord> = {}): CompoundRecord => ({
    id, personId: 'person-1', name, status: 'Active', category: 'Supplement', startDate: '2026-01-03T00:00:00Z',
    endDate: null, notes: '', sourceType: 'Manual', verificationState: 'unverified', categorySource: 'user',
    includeInProviderSummary: true, ...extra,
  });

  const withWriteText = () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    return writeText;
  };

  it('is unchanged when no live items are supplied (no labels, no other-items section)', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.queryByText(/user-entered$/)).not.toBeInTheDocument();
    expect(screen.queryByText('Other items you take')).not.toBeInTheDocument();
  });

  it('labels protocol items by how BioStack knows them: In BioStack or Unverified, always "user-entered"', () => {
    const protocol = makeSavedProviderSummaryProtocol();
    render(
      <ProviderObservationalSummary
        protocol={protocol}
        currentItems={[live('compound-1', 'Magnesium glycinate', { catalogSlug: 'magnesium', verificationState: 'catalog', categorySource: 'catalog' })]}
        generatedAt={providerSummaryGeneratedAt}
      />,
    );
    expect(screen.getByText('In BioStack — user-entered')).toBeInTheDocument();

    render(<ProviderObservationalSummary protocol={protocol} currentItems={[live('compound-1', 'Magnesium glycinate')]} generatedAt={providerSummaryGeneratedAt} />);
    expect(screen.getByText('Unverified — user-entered')).toBeInTheDocument();
  });

  it('adds "Other items you take" from active items outside the saved protocol, labelled and verbatim', async () => {
    const writeText = withWriteText();
    render(
      <ProviderObservationalSummary
        protocol={makeSavedProviderSummaryProtocol()}
        currentItems={[
          live('compound-1', 'Magnesium glycinate'),
          live('x-1', 'My herbal blend', { doseAmount: 3, doseUnit: 'drop', doseFrequency: 'evenings' }),
          live('x-2', 'Creatine', { catalogSlug: 'creatine', verificationState: 'catalog', categorySource: 'catalog' }),
          live('x-3', 'Old paused thing', { status: 'Paused' }),
          live('x-4', 'Private item', { includeInProviderSummary: false }),
        ]}
        generatedAt={providerSummaryGeneratedAt}
      />,
    );

    const section = screen.getByText('Other items you take').closest('section')!;
    expect(within(section).getByText('My herbal blend')).toBeInTheDocument();
    expect(within(section).getByText('Unverified — user-entered')).toBeInTheDocument();
    expect(within(section).getByText('3 drop · evenings')).toBeInTheDocument();
    expect(within(section).getByText('No BioStack data for this item.')).toBeInTheDocument();
    expect(within(section).getByText('Creatine')).toBeInTheDocument();
    expect(within(section).getByText('In BioStack — user-entered')).toBeInTheDocument();
    expect(within(section).queryByText('Old paused thing')).not.toBeInTheDocument();
    expect(within(section).queryByText('Private item')).not.toBeInTheDocument();
    // the user's blank dose stays blank; nothing is filled in
    expect(within(section.querySelectorAll('article')[1] as HTMLElement).getByText('Not recorded in this saved protocol snapshot.')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Copy summary' }));
    const text = writeText.mock.calls[0][0] as string;
    expect(text).toContain('Other items you take');
    expect(text).toContain('My herbal blend (Active; Unverified — user-entered) — User-entered dose: 3 drop');
    expect(text).toContain('No BioStack data for this item.');
    expect(text).not.toContain('Old paused thing');
    expect(text).not.toContain('Private item');
  });

  it('leaves an item out everywhere when the user switched it off, including its logged amounts', async () => {
    const writeText = withWriteText();
    render(
      <ProviderObservationalSummary
        protocol={makeSavedProviderSummaryProtocol()}
        currentItems={[live('compound-1', 'Magnesium glycinate', { includeInProviderSummary: false })]}
        doseLog={[{ id: 'l1', compoundId: 'compound-1', compoundName: 'Magnesium glycinate', logDate: '2026-01-05', takenAtUtc: '2026-01-05T08:00:00Z', amount: 2, unit: 'capsule' }]}
        generatedAt={providerSummaryGeneratedAt}
      />,
    );

    expect(screen.getByText('No active substances recorded')).toBeInTheDocument();
    expect(screen.getByText('No logged amounts recorded')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Copy summary' }));
    expect(writeText.mock.calls[0][0]).not.toContain('Magnesium glycinate (');
  });
});
