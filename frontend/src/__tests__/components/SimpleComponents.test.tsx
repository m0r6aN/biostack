import { ConversionCalc } from '@/components/calculators/ConversionCalc';
import { ReconstitutionCalc } from '@/components/calculators/ReconstitutionCalc';
import { UnifiedDosingCalculator } from '@/components/calculators/UnifiedDosingCalculator';
import { VolumeCalc } from '@/components/calculators/VolumeCalc';
import { CheckInHistory } from '@/components/checkins/CheckInHistory';
import { CompoundList } from '@/components/compounds/CompoundList';
import { CompoundStatusBadge } from '@/components/compounds/CompoundStatusBadge';
import { OverlapFlagsBanner } from '@/components/dashboard/OverlapFlagsBanner';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { GoalBadge } from '@/components/goals/GoalBadge';
import { GoalDisplay } from '@/components/goals/GoalDisplay';
import { EvidenceTierBadge } from '@/components/knowledge/EvidenceTierBadge';
import { LoadingSkeleton } from '@/components/LoadingState';
import { AffiliateDisclosure } from '@/components/recommendations/AffiliateDisclosure';
import { RecommendationCard } from '@/components/recommendations/RecommendationCard';
import { SafetyDisclaimer } from '@/components/SafetyDisclaimer';
import { SuggestionCard } from '@/components/suggestions/SuggestionCard';
import { TimelineEventCard } from '@/components/timeline/TimelineEventCard';
import { TimelineFilter } from '@/components/timeline/TimelineFilter';
import { BrandGlowCard } from '@/components/ui/BrandGlowCard';
import type { EarnedSuggestion } from '@/lib/earnedSuggestions';
import type { ContextualRecommendation } from '@/lib/recommendations';
import type { CalculatorResult, CheckIn, CompoundRecord, GoalDefinition, InteractionFlag, TimelineEvent } from '@/lib/types';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

// ─── EmptyState ────────────────────────────────────────────────────────────────

describe('EmptyState', () => {
  it('renders title and description', () => {
    render(<EmptyState title="Nothing here" description="Add something to get started." />);
    expect(screen.getByText('Nothing here')).toBeInTheDocument();
    expect(screen.getByText('Add something to get started.')).toBeInTheDocument();
  });

  it('renders icon when provided', () => {
    render(<EmptyState title="T" description="D" icon={<span data-testid="icon">🧬</span>} />);
    expect(screen.getByTestId('icon')).toBeInTheDocument();
  });

  it('renders action button and calls onClick', () => {
    const onClick = vi.fn();
    render(<EmptyState title="T" description="D" action={{ label: 'Add item', onClick }} />);
    fireEvent.click(screen.getByText('Add item'));
    expect(onClick).toHaveBeenCalledOnce();
  });
});

// ─── ErrorState ────────────────────────────────────────────────────────────────

describe('ErrorState', () => {
  it('renders with default title and message', () => {
    render(<ErrorState message="Something went wrong." />);
    expect(screen.getByText('Error')).toBeInTheDocument();
    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  it('renders custom title', () => {
    render(<ErrorState title="Load Failed" message="Could not load data." />);
    expect(screen.getByText('Load Failed')).toBeInTheDocument();
  });

  it('renders retry button and calls onRetry', () => {
    const onRetry = vi.fn();
    render(<ErrorState message="Oops" onRetry={onRetry} />);
    fireEvent.click(screen.getByText('Try Again'));
    expect(onRetry).toHaveBeenCalledOnce();
  });
});

// ─── LoadingSkeleton ───────────────────────────────────────────────────────────

describe('LoadingSkeleton', () => {
  it('renders 3 skeleton rows', () => {
    const { container } = render(<LoadingSkeleton />);
    const rows = container.querySelectorAll('.animate-pulse');
    expect(rows.length).toBe(3);
  });
});

// ─── CompoundStatusBadge ───────────────────────────────────────────────────────

describe('CompoundStatusBadge', () => {
  it('renders status label', () => {
    render(<CompoundStatusBadge status="Active" />);
    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('renders planned status', () => {
    render(<CompoundStatusBadge status="Planned" />);
    expect(screen.getByText('Planned')).toBeInTheDocument();
  });
});

// ─── BrandGlowCard ─────────────────────────────────────────────────────────────

describe('BrandGlowCard', () => {
  it('renders children', () => {
    render(<BrandGlowCard><span>Content</span></BrandGlowCard>);
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('applies soft glow by default', () => {
    const { container } = render(<BrandGlowCard>Card</BrandGlowCard>);
    expect(container.firstChild).toHaveClass('shadow-brand-glow');
  });

  it('applies strong glow when specified', () => {
    const { container } = render(<BrandGlowCard glow="strong">Card</BrandGlowCard>);
    expect(container.firstChild).toHaveClass('shadow-brand-glow-strong');
  });
});

// ─── OverlapFlagsBanner ────────────────────────────────────────────────────────

describe('OverlapFlagsBanner', () => {
  it('renders nothing when flags are empty', () => {
    const { container } = render(<OverlapFlagsBanner flags={[]} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders banner with one flag', () => {
    const flags: InteractionFlag[] = [{
      compoundNames: ['CompoundA', 'CompoundB'],
      overlapType: 'Redundant',
      pathwayTag: 'mTOR',
      description: 'Both target mTOR',
      evidenceConfidence: 'High',
    }];
    render(<OverlapFlagsBanner flags={flags} />);
    expect(screen.getByText(/1 pathway overlap detected/i)).toBeInTheDocument();
  });

  it('renders plural text for multiple flags', () => {
    const flags: InteractionFlag[] = [
      { compoundNames: ['A', 'B'], overlapType: 'X', pathwayTag: 'p1', description: '', evidenceConfidence: '' },
      { compoundNames: ['C', 'D'], overlapType: 'X', pathwayTag: 'p2', description: '', evidenceConfidence: '' },
    ];
    render(<OverlapFlagsBanner flags={flags} />);
    expect(screen.getByText(/2 pathway overlap flags detected/i)).toBeInTheDocument();
  });
});

// ─── CompoundList ──────────────────────────────────────────────────────────────

const makeCompound = (overrides: Partial<CompoundRecord> = {}): CompoundRecord => ({
  id: 'c1',
  personId: 'p1',
  name: 'TestCompound',
  category: 'Peptide',
  startDate: '2024-01-01',
  endDate: null,
  status: 'Active',
  notes: '',
  sourceType: 'Manual',
  ...overrides,
});

describe('CompoundList', () => {
  it('renders nothing when list is empty', () => {
    const { container } = render(<CompoundList compounds={[]} />);
    // Should render an empty wrapper div only
    expect(container.querySelector('h4')).toBeNull();
  });

  it('renders compound name and category', () => {
    render(<CompoundList compounds={[makeCompound()]} />);
    expect(screen.getByText('TestCompound')).toBeInTheDocument();
    expect(screen.getByText('Peptide')).toBeInTheDocument();
  });

  it('calls onSelect when a compound row is clicked', () => {
    const onSelect = vi.fn();
    const compound = makeCompound();
    render(<CompoundList compounds={[compound]} onSelect={onSelect} />);
    fireEvent.click(screen.getByText('TestCompound'));
    expect(onSelect).toHaveBeenCalledWith(compound);
  });
});


// ─── EvidenceTierBadge ─────────────────────────────────────────────────────────

describe('EvidenceTierBadge', () => {
  it('renders "Strong Evidence" label for strong tier', () => {
    render(<EvidenceTierBadge tier="strong" />);
    expect(screen.getByText('Strong Evidence')).toBeInTheDocument();
  });

  it('renders "Moderate Evidence" label for moderate tier', () => {
    render(<EvidenceTierBadge tier="moderate" />);
    expect(screen.getByText('Moderate Evidence')).toBeInTheDocument();
  });

  it('falls back to raw tier value for unknown tiers', () => {
    render(<EvidenceTierBadge tier="experimental" />);
    expect(screen.getByText('experimental')).toBeInTheDocument();
  });
});

// ─── AffiliateDisclosure ───────────────────────────────────────────────────────

describe('AffiliateDisclosure', () => {
  it('renders disclosure text', () => {
    render(<AffiliateDisclosure />);
    expect(screen.getByText(/affiliate links/i)).toBeInTheDocument();
  });

  it('applies custom className', () => {
    const { container } = render(<AffiliateDisclosure className="my-custom-class" />);
    expect(container.firstChild).toHaveClass('my-custom-class');
  });
});

// ─── RecommendationCard ────────────────────────────────────────────────────────

const makeRecommendation = (overrides: Partial<ContextualRecommendation> = {}): ContextualRecommendation => ({
  id: 'rec1',
  displayName: 'Creatine',
  contextTags: ['performance'],
  shortDescriptor: 'A popular performance supplement.',
  affiliateUrl: 'https://example.com/creatine',
  disclosureRequired: false,
  ...overrides,
});

describe('RecommendationCard', () => {
  it('renders recommendation name and descriptor', () => {
    render(<RecommendationCard recommendation={makeRecommendation()} />);
    expect(screen.getByText('Creatine')).toBeInTheDocument();
    expect(screen.getByText('A popular performance supplement.')).toBeInTheDocument();
  });

  it('renders whyItAppears when provided', () => {
    render(<RecommendationCard recommendation={makeRecommendation({ whyItAppears: 'Relevant to your context.' })} />);
    expect(screen.getByText('Relevant to your context.')).toBeInTheDocument();
  });

  it('renders source name when provided', () => {
    render(<RecommendationCard recommendation={makeRecommendation({ sourceName: 'Amazon' })} />);
    expect(screen.getByText(/Amazon/)).toBeInTheDocument();
  });
});

// ─── SuggestionCard ───────────────────────────────────────────────────────────

const makeSuggestion = (overrides: Partial<EarnedSuggestion> = {}): EarnedSuggestion => ({
  type: 'clarify_signal',
  title: 'Keep the signal steady',
  explanation: 'Continuing helps isolate the signal.',
  reasoning: 'Based on a weak 7-day review.',
  ...overrides,
});

describe('SuggestionCard', () => {
  it('renders suggestion title and explanation', () => {
    render(<SuggestionCard suggestion={makeSuggestion()} />);
    expect(screen.getByText('Keep the signal steady')).toBeInTheDocument();
    expect(screen.getByText('Continuing helps isolate the signal.')).toBeInTheDocument();
  });

  it('calls onDismiss when "Not now" is clicked', () => {
    const onDismiss = vi.fn();
    render(<SuggestionCard suggestion={makeSuggestion()} onDismiss={onDismiss} />);
    fireEvent.click(screen.getByText('Not now'));
    expect(onDismiss).toHaveBeenCalledOnce();
  });
});

// ─── TimelineFilter ────────────────────────────────────────────────────────────

describe('TimelineFilter', () => {
  it('renders all filter buttons', () => {
    render(<TimelineFilter activeFilter="all" onFilterChange={() => {}} />);
    expect(screen.getByText('All Events')).toBeInTheDocument();
    expect(screen.getByText('Check-ins')).toBeInTheDocument();
  });

  it('calls onFilterChange when a filter is clicked', () => {
    const onChange = vi.fn();
    render(<TimelineFilter activeFilter="all" onFilterChange={onChange} />);
    fireEvent.click(screen.getByText('Check-ins'));
    expect(onChange).toHaveBeenCalledWith('check_in');
  });
});

// ─── TimelineEventCard ─────────────────────────────────────────────────────────

const makeTimelineEvent = (): TimelineEvent => ({
  id: 'ev1',
  personId: 'p1',
  eventType: 'check_in',
  title: 'Day 5 Check-in',
  description: 'Energy was high.',
  occurredAtUtc: '2024-06-15T08:00:00Z',
  relatedEntityId: null,
  relatedEntityType: null,
});

describe('TimelineEventCard', () => {
  it('renders event title and description', () => {
    render(<TimelineEventCard event={makeTimelineEvent()} />);
    expect(screen.getByText('Day 5 Check-in')).toBeInTheDocument();
    expect(screen.getByText('Energy was high.')).toBeInTheDocument();
  });
});

// ─── GoalBadge ────────────────────────────────────────────────────────────────

const makeGoal = (overrides: Partial<GoalDefinition> = {}): GoalDefinition => ({
  id: 'g1',
  name: 'Improve Recovery',
  category: 'recovery',
  description: 'Enhance muscle recovery',
  isActive: true,
  ...overrides,
});

describe('GoalBadge', () => {
  it('renders goal name', () => {
    render(<GoalBadge goal={makeGoal()} />);
    expect(screen.getByText('Improve Recovery')).toBeInTheDocument();
  });

  it('calls onRemove when remove button is clicked', () => {
    const onRemove = vi.fn();
    render(<GoalBadge goal={makeGoal()} onRemove={onRemove} />);
    fireEvent.click(screen.getByText('x'));
    expect(onRemove).toHaveBeenCalledOnce();
  });
});

// ─── GoalDisplay ──────────────────────────────────────────────────────────────

describe('GoalDisplay', () => {
  it('renders empty state when no goals', () => {
    render(<GoalDisplay goals={[]} />);
    expect(screen.getByText('No goals set')).toBeInTheDocument();
  });

  it('renders goal name when goals are provided', () => {
    render(<GoalDisplay goals={[makeGoal()]} />);
    expect(screen.getByText('Improve Recovery')).toBeInTheDocument();
  });
});


// ─── SafetyDisclaimer ─────────────────────────────────────────────────────────

describe('SafetyDisclaimer', () => {
  it('renders general disclaimer by default', () => {
    render(<SafetyDisclaimer />);
    expect(screen.getByText(/observational and educational/i)).toBeInTheDocument();
  });

  it('renders calculation disclaimer for type=calculation', () => {
    render(<SafetyDisclaimer type="calculation" />);
    expect(screen.getByText(/mathematical calculation only/i)).toBeInTheDocument();
  });
});

// ─── VolumeCalc ───────────────────────────────────────────────────────────────

describe('VolumeCalc', () => {
  it('renders volume calculator form', () => {
    render(<VolumeCalc onCalculate={vi.fn()} />);
    expect(screen.getByText('Volume Calculator')).toBeInTheDocument();
    expect(screen.getByText(/desired amount/i)).toBeInTheDocument();
  });

  it('calls onCalculate and shows result', async () => {
    const result: CalculatorResult = { input: 100, output: 1.0, unit: 'mL', formula: '100/100', disclaimer: '' };
    const onCalculate = vi.fn().mockResolvedValue(result);
    render(<VolumeCalc onCalculate={onCalculate} />);
    await act(async () => { fireEvent.click(screen.getByText('Calculate')); });
    expect(onCalculate).toHaveBeenCalledOnce();
    expect(screen.getByText(/volume required/i)).toBeInTheDocument();
  });

  it('shows error when onCalculate throws', async () => {
    const onCalculate = vi.fn().mockRejectedValue(new Error('Server error'));
    render(<VolumeCalc onCalculate={onCalculate} />);
    await act(async () => { fireEvent.click(screen.getByText('Calculate')); });
    expect(screen.getByText('Server error')).toBeInTheDocument();
  });
});

// ─── ReconstitutionCalc ───────────────────────────────────────────────────────

describe('ReconstitutionCalc', () => {
  it('renders reconstitution calculator form', () => {
    render(<ReconstitutionCalc onCalculate={vi.fn()} />);
    expect(screen.getByText('Reconstitution Calculator')).toBeInTheDocument();
    expect(screen.getByText(/peptide amount/i)).toBeInTheDocument();
  });

  it('calls onCalculate and shows result', async () => {
    const result: CalculatorResult = { input: 10, output: 10, unit: 'mg/mL', formula: '10/1', disclaimer: '' };
    const onCalculate = vi.fn().mockResolvedValue(result);
    render(<ReconstitutionCalc onCalculate={onCalculate} />);
    await act(async () => { fireEvent.click(screen.getByText('Calculate')); });
    expect(onCalculate).toHaveBeenCalledOnce();
    expect(screen.getByText(/concentration/i)).toBeInTheDocument();
  });
});

// ─── CheckInHistory ───────────────────────────────────────────────────────────

describe('CheckInHistory', () => {
  const makeCheckIn = (overrides: Partial<CheckIn> = {}): CheckIn => ({
    id: 'ci1',
    personId: 'p1',
    protocolRunId: null,
    date: '2024-06-01',
    weight: 80,
    sleepQuality: 7,
    energy: 8,
    appetite: 6,
    recovery: 7,
    giSymptoms: '',
    mood: '',
    notes: '',
    ...overrides,
  });

  it('renders empty list without crashing', () => {
    const { container } = render(<CheckInHistory checkIns={[]} />);
    expect(container.querySelector('.space-y-4')).toBeInTheDocument();
  });

  it('renders check-in entry with energy metric badge', () => {
    render(<CheckInHistory checkIns={[makeCheckIn()]} />);
    expect(screen.getByText('Energy')).toBeInTheDocument();
    expect(screen.getByText('Sleep')).toBeInTheDocument();
  });
});


// ─── ConversionCalc ───────────────────────────────────────────────────────────

describe('ConversionCalc', () => {
  it('renders unit conversion form with value input', () => {
    render(<ConversionCalc onCalculate={vi.fn()} />);
    expect(screen.getByText('Unit Conversion')).toBeInTheDocument();
    expect(screen.getByText('Value')).toBeInTheDocument();
    expect(screen.getByText('From')).toBeInTheDocument();
    expect(screen.getByText('To')).toBeInTheDocument();
  });

  it('calls onCalculate and shows result when Convert clicked', async () => {
    const result: CalculatorResult = { input: 1000, output: 1, unit: 'mg', formula: '1000 mcg → 1 mg', disclaimer: '' };
    const onCalculate = vi.fn().mockResolvedValue(result);
    render(<ConversionCalc onCalculate={onCalculate} />);
    await act(async () => { fireEvent.click(screen.getByText('Convert')); });
    expect(onCalculate).toHaveBeenCalledOnce();
    expect(screen.getByText('Result')).toBeInTheDocument();
  });

  it('shows error message when onCalculate throws', async () => {
    const onCalculate = vi.fn().mockRejectedValue(new Error('Conversion error'));
    render(<ConversionCalc onCalculate={onCalculate} />);
    await act(async () => { fireEvent.click(screen.getByText('Convert')); });
    expect(screen.getByText('Conversion error')).toBeInTheDocument();
  });
});

// ─── UnifiedDosingCalculator ──────────────────────────────────────────────────

describe('UnifiedDosingCalculator', () => {
  it('renders with default title', () => {
    render(<UnifiedDosingCalculator />);
    expect(screen.getByText('Reconstitution & Dosing Calculator')).toBeInTheDocument();
  });

  it('renders with custom title and description', () => {
    render(<UnifiedDosingCalculator title="My Calculator" description="Custom description" />);
    expect(screen.getByText('My Calculator')).toBeInTheDocument();
    expect(screen.getByText('Custom description')).toBeInTheDocument();
  });

  it('shows Attach to Protocol button when onRecord is provided', () => {
    render(<UnifiedDosingCalculator onRecord={vi.fn()} />);
    expect(screen.getByText('Attach to Protocol')).toBeInTheDocument();
  });
});
