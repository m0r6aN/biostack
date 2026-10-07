import { CompoundEditForm } from '@/components/compounds/CompoundEditForm';
import type { CompoundRecord } from '@/lib/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
const record: CompoundRecord = { id: 'a', personId: 'p', name: 'Existing', category: 'Peptide', startDate: '2026-09-01T15:30:01.123Z', endDate: null, status: 'Active', notes: 'Original notes', sourceType: 'Manual', source: 'Protocol Analyzer', goal: 'Existing goal', pricePaid: 12 };
it.each([record.startDate, null])('preserves unchanged original start timestamp %s and provenance on name edit', async startDate => {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  // The API permits a null start date, despite the older frontend record type.
  render(<CompoundEditForm compound={{ ...record, startDate } as CompoundRecord} onSubmit={onSubmit} onCancel={vi.fn()} />);
  fireEvent.change(screen.getByLabelText('Compound name'), { target: { value: 'Renamed' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ name: 'Renamed', startDate, source: record.source, goal: record.goal, pricePaid: record.pricePaid, endDate: null, status: record.status, sourceType: record.sourceType })));
});
it('uses UTC midnight only after an intentional calendar-date edit', async () => {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(<CompoundEditForm compound={record} onSubmit={onSubmit} onCancel={vi.fn()} />);
  fireEvent.change(screen.getByLabelText('Start Date'), { target: { value: '2026-09-03' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ startDate: '2026-09-03T00:00:00.000Z' })));
});
it.each(['Unknown', 'Compound', 'Sarm', 'Serm', 'Hormone'])('preserves existing %s category during unrelated edits', async category => {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(<CompoundEditForm compound={{ ...record, category }} onSubmit={onSubmit} onCancel={vi.fn()} />);
  expect(screen.getByLabelText('Category')).toHaveValue(category);
  fireEvent.change(screen.getByLabelText('Notes'), { target: { value: 'Edited note' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ category })));
});
it('activates Cancel normally with Enter without submitting', async () => {
  const user = userEvent.setup(); const onCancel = vi.fn(); const onSubmit = vi.fn();
  render(<CompoundEditForm compound={record} onSubmit={onSubmit} onCancel={onCancel} />);
  screen.getByRole('button', { name: 'Cancel' }).focus();
  await user.keyboard('{Enter}');
  expect(onCancel).toHaveBeenCalledOnce(); expect(onSubmit).not.toHaveBeenCalled();
});
