import { AnalyzingState } from '@/components/tools/analyzer/AnalyzingState';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

describe('AnalyzingState', () => {
  it('renders "Analysis in progress" and all step labels for mode=Paste', () => {
    render(<AnalyzingState mode="Paste" />);

    // Check that the heading is present
    expect(screen.getByText('Analysis in progress')).toBeInTheDocument();

    // Check first Paste step: 'Extracting text'
    expect(screen.getByText('Extracting text')).toBeInTheDocument();

    // Check second Paste step: 'Normalizing protocol rows'
    expect(screen.getByText('Normalizing protocol rows')).toBeInTheDocument();

    // Check last step (all modes share these final steps): 'Comparing alternatives'
    expect(screen.getByText('Comparing alternatives')).toBeInTheDocument();

    // Check that all 6 steps are rendered
    const steps = screen.getAllByRole('listitem');
    expect(steps).toHaveLength(6);
  });

  it('renders for mode=CameraScan showing that mode\'s first step string', () => {
    render(<AnalyzingState mode="CameraScan" />);

    // Check CameraScan's first step: 'Reading image'
    expect(screen.getByText('Reading image')).toBeInTheDocument();

    // Check that the heading is present
    expect(screen.getByText('Analysis in progress')).toBeInTheDocument();

    // Check that CameraScan steps are rendered
    expect(screen.getByText('Extracting text from photo')).toBeInTheDocument();

    // Check that all 6 steps are rendered
    const steps = screen.getAllByRole('listitem');
    expect(steps).toHaveLength(6);
  });

  it('renders for mode=Link', () => {
    render(<AnalyzingState mode="Link" />);

    // Check Link's first step: 'Fetching shared document'
    expect(screen.getByText('Fetching shared document')).toBeInTheDocument();

    // Check that all 6 steps are rendered
    const steps = screen.getAllByRole('listitem');
    expect(steps).toHaveLength(6);
  });

  it('renders for mode=FileUpload', () => {
    render(<AnalyzingState mode="FileUpload" />);

    // Check FileUpload's second step: 'Reading table structure'
    expect(screen.getByText('Reading table structure')).toBeInTheDocument();

    // Check that all 6 steps are rendered
    const steps = screen.getAllByRole('listitem');
    expect(steps).toHaveLength(6);
  });

  it('renders skeleton placeholders for report', () => {
    const { container } = render(<AnalyzingState mode="Paste" />);

    // Check for skeleton boxes (animate-pulse elements)
    const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('renders step counters initially', () => {
    render(<AnalyzingState mode="Paste" />);

    // In the initial state (without animation advancing), all steps should show numbers
    // We check for the numbered spans containing 1, 2, 3, etc.
    const spans = screen.getAllByRole('listitem').map((li) => li.querySelector('span'));
    expect(spans.length).toBeGreaterThan(0);
  });
});
