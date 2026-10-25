import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SubmitClaimForm } from '@/components/check/SubmitClaimForm';

afterEach(() => vi.unstubAllGlobals());

describe('SubmitClaimForm', () => {
  it('has no URL, creator, photo or dose fields', () => {
    const { container } = render(<SubmitClaimForm compounds={[{ slug: 'bpc-157', name: 'BPC-157' }]} initialClaim="heals tendons" initialCompound="bpc-157" />);
    expect(container.querySelector('input[type="url"], input[type="file"], input[name*="dose" i], input[name*="creator" i], input[name*="url" i]')).toBeNull();
    expect(screen.getByLabelText(/The claim/)).toHaveValue('heals tendons');
  });

  it('shows the safety message and redaction notice the API returns', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(new Response(JSON.stringify({
      status: 'queued', redacted: true, notice: 'We stripped the dose numbers and seller details. That\u2019s policy.',
      safetyMessage: 'If this is happening now, contact emergency services or poison control.',
    }), { status: 202 })));
    vi.stubGlobal('fetch', fetchMock);
    render(<SubmitClaimForm compounds={[{ slug: 'bpc-157', name: 'BPC-157' }]} initialClaim="chest pain" initialCompound="bpc-157" />);
    fireEvent.change(screen.getByLabelText('Where you saw it'), { target: { value: 'reddit' } });
    fireEvent.click(screen.getByRole('button', { name: 'Send it in' }));
    expect(await screen.findByText(/contact emergency services or poison control/)).toBeInTheDocument();
    expect(screen.getByText(/We stripped the dose numbers/)).toBeInTheDocument();
    const body = JSON.parse((fetchMock.mock.calls[0] as unknown as [string, RequestInit])[1].body as string);
    expect(body).toMatchObject({ compoundSlug: 'bpc-157', claim: 'chest pain', seenOn: 'reddit' });
  });
});
