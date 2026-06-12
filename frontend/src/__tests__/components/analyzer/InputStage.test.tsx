import React from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { InputStage } from '@/components/tools/analyzer/InputStage';
import type { AnalyzerGoalSelection } from '@/lib/analyzerGoals';
import type { AnalyzerContextFields } from '@/components/tools/analyzer/useAnalyzerSession';

// --- Next.js Link mock (same as RefineAnalysisPanel test) ---
vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

// ── Default props ─────────────────────────────────────────────────────────────

const EMPTY_GOALS: AnalyzerGoalSelection = { primaryCategory: null, refinementGoalIds: [] };
const EMPTY_CONTEXT: AnalyzerContextFields = { sex: '', age: '', weight: '', existingStack: '' };

function makeProps(overrides: Partial<React.ComponentProps<typeof InputStage>> = {}): React.ComponentProps<typeof InputStage> {
  return {
    mode: 'Paste',
    inputText: '',
    linkUrl: '',
    selectedFile: null,
    goals: EMPTY_GOALS,
    context: EMPTY_CONTEXT,
    profile: null,
    isAuthenticated: false,
    isPending: false,
    error: '',
    onModeChange: vi.fn(),
    onInputTextChange: vi.fn(),
    onLinkUrlChange: vi.fn(),
    onFileSelected: vi.fn(),
    onGoalsChange: vi.fn(),
    onContextChange: vi.fn(),
    onAnalyze: vi.fn(),
    onClear: vi.fn(),
    onLoadExample: vi.fn(),
    onScanRequested: vi.fn(),
    ...overrides,
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('InputStage', () => {
  it('renders all four mode tabs', () => {
    render(<InputStage {...makeProps()} />);
    expect(screen.getByRole('button', { name: 'Paste' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Scan' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Link' })).toBeInTheDocument();
  });

  it('clicking Upload tab calls onModeChange with FileUpload', () => {
    const onModeChange = vi.fn();
    render(<InputStage {...makeProps({ onModeChange })} />);
    fireEvent.click(screen.getByRole('button', { name: 'Upload' }));
    expect(onModeChange).toHaveBeenCalledWith('FileUpload');
  });

  it('clicking Scan tab calls onModeChange with CameraScan', () => {
    const onModeChange = vi.fn();
    render(<InputStage {...makeProps({ onModeChange })} />);
    fireEvent.click(screen.getByRole('button', { name: 'Scan' }));
    expect(onModeChange).toHaveBeenCalledWith('CameraScan');
  });

  it('clicking Link tab calls onModeChange with Link', () => {
    const onModeChange = vi.fn();
    render(<InputStage {...makeProps({ onModeChange })} />);
    fireEvent.click(screen.getByRole('button', { name: 'Link' }));
    expect(onModeChange).toHaveBeenCalledWith('Link');
  });

  it('Paste mode renders textarea with correct value', () => {
    render(<InputStage {...makeProps({ mode: 'Paste', inputText: 'BPC-157 500mcg daily' })} />);
    const textarea = screen.getByRole('textbox', { name: /protocol text/i });
    expect(textarea).toBeInTheDocument();
    expect(textarea).toHaveValue('BPC-157 500mcg daily');
  });

  it('typing in Paste textarea calls onInputTextChange', () => {
    const onInputTextChange = vi.fn();
    render(<InputStage {...makeProps({ mode: 'Paste', inputText: '', onInputTextChange })} />);
    const textarea = screen.getByRole('textbox', { name: /protocol text/i });
    fireEvent.change(textarea, { target: { value: 'NAD+ 250mg daily' } });
    expect(onInputTextChange).toHaveBeenCalledWith('NAD+ 250mg daily');
  });

  it('Analyze button is disabled when mode=Paste and inputText is empty', () => {
    render(<InputStage {...makeProps({ mode: 'Paste', inputText: '' })} />);
    expect(screen.getByRole('button', { name: /analyze protocol/i })).toBeDisabled();
  });

  it('Analyze button is enabled when mode=Paste and inputText is non-empty', () => {
    render(<InputStage {...makeProps({ mode: 'Paste', inputText: 'BPC-157 500mcg daily' })} />);
    expect(screen.getByRole('button', { name: /analyze protocol/i })).not.toBeDisabled();
  });

  it('clicking Analyze when enabled calls onAnalyze', () => {
    const onAnalyze = vi.fn();
    render(<InputStage {...makeProps({ mode: 'Paste', inputText: 'BPC-157 500mcg', onAnalyze })} />);
    fireEvent.click(screen.getByRole('button', { name: /analyze protocol/i }));
    expect(onAnalyze).toHaveBeenCalledTimes(1);
  });

  it('clicking example Healing stack calls onLoadExample with "healing"', () => {
    const onLoadExample = vi.fn();
    render(<InputStage {...makeProps({ onLoadExample })} />);
    // There may be multiple instances (buttons + example section) — click the first
    const healingBtns = screen.getAllByRole('button', { name: /healing stack/i });
    fireEvent.click(healingBtns[0]);
    expect(onLoadExample).toHaveBeenCalledWith('healing');
  });

  it('when error prop is set, the failure card renders', () => {
    render(<InputStage {...makeProps({ error: 'Something went wrong.' })} />);
    expect(screen.getByText('Analysis is temporarily unavailable.')).toBeInTheDocument();
    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  it('failure card retry button calls onAnalyze', () => {
    const onAnalyze = vi.fn();
    render(<InputStage {...makeProps({ error: 'Oops', onAnalyze })} />);
    fireEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(onAnalyze).toHaveBeenCalledTimes(1);
  });

  it('renders the trust copy string verbatim', () => {
    render(<InputStage {...makeProps()} />);
    expect(screen.getByText('Educational analysis only. Verify all dosing math manually.')).toBeInTheDocument();
  });

  it('shows Analyzing Protocol... text while isPending', () => {
    render(<InputStage {...makeProps({ inputText: 'something', isPending: true })} />);
    expect(screen.getByRole('button', { name: /analyzing protocol/i })).toBeInTheDocument();
  });
});
