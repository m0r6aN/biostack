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
});
