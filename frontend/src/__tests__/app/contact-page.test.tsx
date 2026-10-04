import ContactPage from '@/app/contact/page';
import { fireEvent, render, screen, within } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn(), prefetch: vi.fn() }),
  usePathname: () => '/contact',
  useSearchParams: () => new URLSearchParams(''),
}));

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <h1>{title}</h1>,
}));

const fetchMock = vi.fn();

describe('contact page', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    fetchMock.mockResolvedValue({ ok: true, status: 200, json: async () => ({ ok: true }) });
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders the form with the five categories and shows success after a posted message', async () => {
    render(<ContactPage />);

    expect(screen.getByRole('heading', { name: 'Contact Us' })).toBeInTheDocument();
    expect(screen.getByLabelText('Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Message')).toBeInTheDocument();

    const categorySelect = screen.getByLabelText('Category');
    expect(
      within(categorySelect)
        .getAllByRole('option')
        .map((option) => option.textContent),
    ).toEqual(['Support', 'Billing', 'Feedback', 'Partnership', 'Other']);

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Ada Lovelace' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'ada@example.com' } });
    fireEvent.change(categorySelect, { target: { value: 'Billing' } });
    fireEvent.change(screen.getByLabelText('Message'), {
      target: { value: 'A question about my invoice.' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    await screen.findByText(/Thanks for reaching out/);
    expect(screen.queryByRole('button', { name: 'Send message' })).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/contact',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: JSON.stringify({
          name: 'Ada Lovelace',
          email: 'ada@example.com',
          category: 'Billing',
          message: 'A question about my invoice.',
        }),
      }),
    );
  });
});
