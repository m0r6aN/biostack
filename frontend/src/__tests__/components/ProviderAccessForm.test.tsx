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

  it('renders the conservative upper-bound maxLength on each contact field', () => {
    render(<ProviderAccessForm />);

    expect(screen.getByLabelText('Name')).toHaveAttribute('maxLength', '160');
    expect(screen.getByLabelText('Role')).toHaveAttribute('maxLength', '120');
    expect(screen.getByLabelText('Organization')).toHaveAttribute('maxLength', '200');
    expect(screen.getByLabelText('Work email')).toHaveAttribute('maxLength', '255');
  });

  it('submits raw untrimmed values at the supported name/role boundary lengths', async () => {
    vi.mocked(apiClient.requestProviderAccess).mockResolvedValue({
      requestId: 'request-2',
      status: 'pending',
      submittedAtUtc: '2026-07-11T12:00:00Z',
    });
    render(<ProviderAccessForm />);

    const boundaryName = `${'A'.repeat(158)}  `;
    const boundaryRole = `${'B'.repeat(118)}  `;

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: boundaryName } });
    fireEvent.change(screen.getByLabelText('Work email'), { target: { value: 'jordan@example.com' } });
    fireEvent.change(screen.getByLabelText('Organization'), { target: { value: 'Example Practice' } });
    fireEvent.change(screen.getByLabelText('Role'), { target: { value: boundaryRole } });
    fireEvent.click(screen.getByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: 'Request provider access' }));

    await waitFor(() => expect(apiClient.requestProviderAccess).toHaveBeenCalledWith({
      email: 'jordan@example.com',
      name: boundaryName,
      organization: 'Example Practice',
      role: boundaryRole,
      consent: true,
      website: '',
    }));
    expect(await screen.findByRole('status')).toHaveTextContent('Request received');
  });
});
