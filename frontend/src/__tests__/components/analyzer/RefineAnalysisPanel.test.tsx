import React from 'react';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { RefineAnalysisPanel } from '@/components/tools/analyzer/RefineAnalysisPanel';
import type { AnalyzerContextFields } from '@/components/tools/analyzer/useAnalyzerSession';
import type { PersonProfile } from '@/lib/types';

// --- Next.js Link mock ---
vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

const EMPTY_CONTEXT: AnalyzerContextFields = { sex: '', age: '', weight: '', existingStack: '' };

const FILLED_CONTEXT: AnalyzerContextFields = { sex: 'male', age: '35', weight: '80', existingStack: '' };

const MOCK_PROFILE: PersonProfile = {
  id: 'p1',
  displayName: 'Test User',
  sex: 'female',
  age: 30,
  weight: 65,
  notes: '',
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
};

function collectAnalyzerEvents(): string[] {
  const events: string[] = [];
  const handler = ((e: CustomEvent) => {
    events.push(e.detail.eventName);
  }) as EventListener;
  window.addEventListener('biostack:analyzer_event', handler);
  return events;
}

describe('RefineAnalysisPanel', () => {
  let events: string[] = [];
  let handler: EventListener;

  beforeEach(() => {
    events = [];
    handler = ((e: CustomEvent) => {
      events.push(e.detail.eventName);
    }) as EventListener;
    window.addEventListener('biostack:analyzer_event', handler);
  });

  afterEach(() => {
    window.removeEventListener('biostack:analyzer_event', handler);
  });

  it('is collapsed by default: sex select and age input are not in the DOM', () => {
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={() => {}}
        profile={null}
        isAuthenticated={false}
      />,
    );
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/age/i)).not.toBeInTheDocument();
    // The toggle button should be visible
    expect(screen.getByRole('button', { name: /refine analysis/i })).toBeInTheDocument();
  });

  it('clicking the toggle expands the fields and fires analyzer_context_opened once', async () => {
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={() => {}}
        profile={null}
        isAuthenticated={false}
      />,
    );

    const toggle = screen.getByRole('button', { name: /refine analysis/i });
    fireEvent.click(toggle);

    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    expect(events).toContain('analyzer_context_opened');
    expect(events.filter(e => e === 'analyzer_context_opened')).toHaveLength(1);

    // Click again to collapse and re-expand — should NOT fire a second time
    fireEvent.click(toggle);
    fireEvent.click(toggle);
    expect(events.filter(e => e === 'analyzer_context_opened')).toHaveLength(1);
  });

  it('with a profile and empty context: onChange called with profile values, badge shown, prefilled event fired', async () => {
    const onChange = vi.fn();
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={onChange}
        profile={MOCK_PROFILE}
        isAuthenticated={true}
      />,
    );

    // onChange should have been called on mount with profile data
    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith({
        sex: 'female',
        age: '30',
        weight: '65',
        existingStack: '',
      });
    });

    expect(events).toContain('analyzer_context_prefilled');

    // Expand to see the badge
    fireEvent.click(screen.getByRole('button', { name: /refine analysis/i }));
    await waitFor(() => {
      expect(screen.getByText(/from your profile/i)).toBeInTheDocument();
    });
  });

  it('anonymous user (isAuthenticated=false, profile=null): sign-in nudge link is shown after expand', async () => {
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={() => {}}
        profile={null}
        isAuthenticated={false}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /refine analysis/i }));

    await waitFor(() => {
      const link = screen.getByRole('link', { name: /create a profile/i });
      expect(link).toBeInTheDocument();
      expect(link).toHaveAttribute('href', '/auth/signin?callbackUrl=/tools/analyzer');
    });
  });

  it('authenticated but no profile (isAuthenticated=true, profile=null): create-profile nudge to /profiles', async () => {
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={() => {}}
        profile={null}
        isAuthenticated={true}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /refine analysis/i }));

    await waitFor(() => {
      const link = screen.getByRole('link', { name: /create a profile/i });
      expect(link).toBeInTheDocument();
      expect(link).toHaveAttribute('href', '/profiles');
    });
  });

  it('typing into the age field calls onChange with the updated context', async () => {
    const onChange = vi.fn();
    // Use a non-empty context and no profile to avoid prefill side-effects
    render(
      <RefineAnalysisPanel
        context={FILLED_CONTEXT}
        onChange={onChange}
        profile={null}
        isAuthenticated={true}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /refine analysis/i }));

    await waitFor(() => {
      expect(screen.getByLabelText(/age/i)).toBeInTheDocument();
    });

    const ageInput = screen.getByLabelText(/age/i);
    fireEvent.change(ageInput, { target: { value: '42' } });

    expect(onChange).toHaveBeenCalledWith({ ...FILLED_CONTEXT, age: '42' });
  });

  it('nudge link fires analyzer_profile_nudge_clicked on click', async () => {
    render(
      <RefineAnalysisPanel
        context={EMPTY_CONTEXT}
        onChange={() => {}}
        profile={null}
        isAuthenticated={false}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /refine analysis/i }));

    await waitFor(() => {
      expect(screen.getByRole('link', { name: /create a profile/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('link', { name: /create a profile/i }));
    expect(events).toContain('analyzer_profile_nudge_clicked');
  });
});
