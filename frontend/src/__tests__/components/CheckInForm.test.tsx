import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CheckInForm } from '@/components/checkins/CheckInForm';

// Mock useSettings so we can control the unit without needing providers
vi.mock('@/lib/settings', () => ({
  useSettings: vi.fn(),
}));

// Mock lbsToKg so we can verify it's called
vi.mock('@/lib/utils', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/lib/utils')>();
  return {
    ...actual,
    lbsToKg: vi.fn((v: number) => actual.lbsToKg(v)),
  };
});

import { useSettings } from '@/lib/settings';
import { lbsToKg } from '@/lib/utils';

const mockUseSettings = useSettings as ReturnType<typeof vi.fn>;

describe('CheckInForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('metric mode', () => {
    beforeEach(() => {
      mockUseSettings.mockReturnValue({ settings: { weightUnit: 'metric' } });
    });

    it('shows metric weight controls', () => {
      render(<CheckInForm personId="p1" onSubmit={vi.fn()} />);
      expect(screen.getByText('Weight')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'kg' })).toBeInTheDocument();
    });

    it('submits weight value as-is (no conversion)', async () => {
      const onSubmit = vi.fn().mockResolvedValue(undefined);
      render(<CheckInForm personId="p1" onSubmit={onSubmit} />);

      const weightInput = screen.getByPlaceholderText(/e\.g\. 77/i);
      await userEvent.clear(weightInput);
      await userEvent.type(weightInput, '80');

      fireEvent.submit(weightInput.closest('form')!);

      await waitFor(() => {
        expect(onSubmit).toHaveBeenCalled();
        const call = onSubmit.mock.calls[0][0];
        expect(call.weight).toBe(80);
      });
    });
  });

  describe('imperial mode', () => {
    beforeEach(() => {
      mockUseSettings.mockReturnValue({ settings: { weightUnit: 'imperial' } });
    });

    it('shows imperial weight controls', () => {
      render(<CheckInForm personId="p1" onSubmit={vi.fn()} />);
      expect(screen.getByText('Weight')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'lbs' })).toBeInTheDocument();
    });

    it('converts lbs to kg before calling onSubmit', async () => {
      const onSubmit = vi.fn().mockResolvedValue(undefined);
      render(<CheckInForm personId="p1" onSubmit={onSubmit} />);

      const weightInput = screen.getByPlaceholderText(/e\.g\. 170/i);
      await userEvent.clear(weightInput);
      await userEvent.type(weightInput, '176');

      fireEvent.submit(weightInput.closest('form')!);

      await waitFor(() => {
        expect(lbsToKg).toHaveBeenCalledWith(176);
        expect(onSubmit).toHaveBeenCalled();
        const submitted = onSubmit.mock.calls[0][0];
        // 176 lbs ≈ 79.83 kg
        expect(submitted.weight).toBeCloseTo(79.83, 0);
      });
    });
  });

  it('shows the submit button', () => {
    mockUseSettings.mockReturnValue({ settings: { weightUnit: 'metric' } });
    render(<CheckInForm personId="p1" onSubmit={vi.fn()} />);
    expect(screen.getByRole('button', { name: /commit daily record/i })).toBeInTheDocument();
  });

  it('disables the button when isLoading=true', () => {
    mockUseSettings.mockReturnValue({ settings: { weightUnit: 'metric' } });
    render(<CheckInForm personId="p1" onSubmit={vi.fn()} isLoading />);
    expect(screen.getByRole('button', { name: /synchronizing/i })).toBeDisabled();
  });
});
