import { ContactCareTeamModal } from '@/components/protocol-portal/ContactCareTeamModal';
import { DashboardTab } from '@/components/protocol-portal/tabs/DashboardTab';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

const today = {
  dateIso: '2026-07-13',
  title: 'Today',
  subtitle: 'Observed schedule',
  items: [],
};

describe('care-team note honesty', () => {
  it('labels the dashboard action as record storage, not messaging', () => {
    render(
      <DashboardTab
        today={today}
        onViewCalendar={vi.fn()}
        onViewLabs={vi.fn()}
        onLogDoses={vi.fn()}
        onSaveCareTeamNote={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: 'Save Care Team Note' })).toBeInTheDocument();
    expect(screen.queryByText(/message care team/i)).not.toBeInTheDocument();
  });

  it('states that saving a note does not notify a care team', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ContactCareTeamModal onClose={vi.fn()} onSubmit={onSubmit} />);

    expect(screen.getByText(/does not send a notification/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/protocol record/i)).toBeInTheDocument();

    await user.type(screen.getByRole('textbox'), '  Observed update  ');
    await user.click(screen.getByRole('button', { name: 'Save Note' }));

    expect(onSubmit).toHaveBeenCalledWith('Observed update');
    expect(await screen.findByRole('heading', { name: 'Note Saved' })).toBeInTheDocument();
    expect(screen.queryByText(/message sent/i)).not.toBeInTheDocument();
  });
});
