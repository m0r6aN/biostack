import ToolsPage from '@/app/tools/page';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav>Navigation</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Footer</footer> }));
vi.mock('@/lib/AuthProvider', () => ({ useAuth: () => ({ user: null }) }));
vi.mock('@/lib/context', () => ({
  useProfile: () => ({ currentProfileId: null, profiles: [], setProfiles: vi.fn(), setCurrentProfileId: vi.fn() }),
}));
vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn().mockResolvedValue([]),
  },
}));

describe('active /tools calculator visuals', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it.each([375, 1280])('keeps the vial, syringe, beginner help, and worded summary visible at %spx', (width) => {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: width });
    render(<ToolsPage />);

    expect(screen.getByTestId('vial-visualizer')).toBeVisible();
    expect(screen.getByTestId('syringe-draw-visualizer')).toBeVisible();
    expect(screen.getByText('How much powder is printed on the vial?')).toBeVisible();
    expect(screen.getByText('How much liquid was added?')).toBeVisible();
    expect(screen.getByText('What amount are you calculating?')).toBeVisible();
    expect(screen.getByRole('heading', { name: 'Calculation summary' })).toBeVisible();
    expect(screen.getByText(/5 mg in 2 mL gives a concentration of 2,500 mcg\/mL/)).toBeVisible();
    expect(screen.getByRole('button', { name: 'View vial measurement guide' })).toBeEnabled();
  });

  it('keeps both visuals prominent after switching to mix mode', () => {
    render(<ToolsPage />);
    fireEvent.click(screen.getByRole('button', { name: /Calculate concentration/i }));

    expect(screen.getByTestId('vial-visualizer')).toBeVisible();
    expect(screen.getByTestId('syringe-draw-visualizer')).toBeVisible();
    expect(screen.getByText('This representation separates the entered dry powder from the liquid added.')).toBeVisible();
  });

  it('replaces the syringe meter with a clear incomplete state for zero input', () => {
    render(<ToolsPage />);
    const desiredDose = screen.getByText('What amount are you calculating?').closest('label')!.querySelector('input')!;
    fireEvent.change(desiredDose, { target: { value: '0' } });

    expect(screen.queryByRole('meter')).not.toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('No draw is shown');
  });

  it('flags unit mismatch and order-of-magnitude values without recommending a dose', () => {
    render(<ToolsPage />);
    const amountField = screen.getByText('What amount are you calculating?').closest('label')!;
    fireEvent.change(amountField.querySelector('input')!, { target: { value: '5' } });
    fireEvent.change(amountField.querySelector('select')!, { target: { value: 'g' } });

    expect(screen.getByText(/vial and calculated amount use different units/i)).toBeVisible();
    expect(screen.getByText(/differs from the vial amount by a large order of magnitude/i)).toBeVisible();
    expect(screen.queryByText(/you should|recommended|take|inject/i)).not.toBeInTheDocument();
  });
});
