import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HelpTip } from '@/components/ui/HelpTip';

describe('HelpTip', () => {
  it('renders children and shows tooltip on click, hides on second click', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /synergy/i }));
    const tip = screen.getByRole('tooltip');
    expect(tip).toBeInTheDocument();
    expect(tip).toHaveTextContent('may support related outcomes');

    await userEvent.click(screen.getByRole('button'));
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('dismisses on Escape key press', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    expect(screen.getByRole('tooltip')).toBeInTheDocument();

    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('toggles via Enter and Space keys', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    const trigger = screen.getByRole('button');
    trigger.focus();

    await userEvent.keyboard('{Enter}');
    expect(screen.getByRole('tooltip')).toBeInTheDocument();

    await userEvent.keyboard(' ');
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('links trigger to tooltip via aria-describedby', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    const tooltip = screen.getByRole('tooltip');
    expect(screen.getByRole('button')).toHaveAttribute('aria-describedby', tooltip.id);
  });

  it('sets aria-expanded correctly', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    const trigger = screen.getByRole('button');
    expect(trigger).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(trigger);
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });

  it('dismisses on outside click', async () => {
    render(
      <div>
        <HelpTip tipKey="synergy">Synergy</HelpTip>
        <button>Outside</button>
      </div>
    );
    await userEvent.click(screen.getByRole('button', { name: /synergy/i }));
    expect(screen.getByRole('tooltip')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /outside/i }));
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('renders the panel on document.body via a portal, immune to an ancestor overflow:hidden clip', async () => {
    const { container } = render(
      <div style={{ overflow: 'hidden', height: 20 }} data-testid="clipping-ancestor">
        <HelpTip tipKey="synergy">Synergy</HelpTip>
      </div>
    );
    await userEvent.click(screen.getByRole('button'));
    const tooltip = screen.getByRole('tooltip');
    expect(container.contains(tooltip)).toBe(false);
    expect(document.body.contains(tooltip)).toBe(true);
  });

  it('flips the panel below the trigger when there is no room above it', async () => {
    const getRectSpy = vi
      .spyOn(HTMLElement.prototype, 'getBoundingClientRect')
      .mockReturnValue({ top: 5, left: 100, right: 200, bottom: 25, width: 100, height: 20, x: 100, y: 5, toJSON() { return {}; } });

    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    const tooltip = screen.getByRole('tooltip');

    expect(tooltip).toHaveAttribute('data-placement', 'bottom');
    // bottom (25) + the trigger gap (8), independent of the panel's own
    // (unmeasurable-in-jsdom) height, since it opens below the trigger.
    expect(tooltip.style.top).toBe('33px');

    getRectSpy.mockRestore();
  });

  it('clamps the panel horizontally so it never runs past the right edge of the viewport', async () => {
    const originalInnerWidth = window.innerWidth;
    Object.defineProperty(window, 'innerWidth', { value: 1024, configurable: true });
    const getRectSpy = vi
      .spyOn(HTMLElement.prototype, 'getBoundingClientRect')
      .mockReturnValue({ top: 400, left: 2000, right: 2100, bottom: 420, width: 100, height: 20, x: 2000, y: 400, toJSON() { return {}; } });

    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    const tooltip = screen.getByRole('tooltip');

    // 1024 (viewport) - 224 (panel width fallback) - 8 (margin) = 792.
    expect(tooltip.style.left).toBe('792px');

    getRectSpy.mockRestore();
    Object.defineProperty(window, 'innerWidth', { value: originalInnerWidth, configurable: true });
  });
});

it('constrains a tall tooltip to the better side and permits keyboard scrolling', async () => {
  vi.stubGlobal('innerHeight', 180);
  const rect = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockReturnValue({ top: 60, bottom: 80, left: 20, right: 100, width: 80, height: 20, x: 20, y: 60, toJSON() { return {}; } });
  const height = vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(140);
  try {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    const tip = screen.getByRole('tooltip');
    expect(tip.style.maxHeight).toBe('84px');
    expect(tip.style.top).toBe('88px');
    expect(tip.style.overflowY).toBe('auto');
    expect(tip).toHaveAttribute('tabindex', '0');
    await userEvent.tab();
    expect(tip).toHaveFocus();
    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    expect(screen.getByRole('button')).toHaveFocus();
  } finally { rect.mockRestore(); height.mockRestore(); vi.unstubAllGlobals(); }
});
