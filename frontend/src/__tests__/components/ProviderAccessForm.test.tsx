import { ProviderAccessForm } from '@/components/marketing/ProviderAccessForm';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: {
    requestProviderAccess: vi.fn(),
  },
}));

describe('ProviderAccessForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('submits contact-only provider pilot data with consent and confirms receipt', async () => {
    vi.mocked(apiClient.requestProviderAccess).mockResolvedValue({
      requestId: 'request-1',
      status: 'pending',
      submittedAtUtc: '2026-07-11T12:00:00Z',
    });
    render(<ProviderAccessForm />);

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Jordan Provider' } });
    fireEvent.change(screen.getByLabelText('Work email'), { target: { value: 'jordan@example.com' } });
    fireEvent.change(screen.getByLabelText('Organization'), { target: { value: 'Example Practice' } });
    fireEvent.change(screen.getByLabelText('Role'), { target: { value: 'Operations lead' } });
    fireEvent.click(screen.getByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: 'Request provider access' }));

    await waitFor(() => expect(apiClient.requestProviderAccess).toHaveBeenCalledWith({
      email: 'jordan@example.com',
      name: 'Jordan Provider',
      organization: 'Example Practice',
      role: 'Operations lead',
      consent: true,
      website: '',
    }));
    expect(await screen.findByRole('status')).toHaveTextContent('Request received');
  });
});
