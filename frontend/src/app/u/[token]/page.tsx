import type { Metadata } from 'next';
import { UnsubscribeConfirm } from '@/components/alerts/UnsubscribeConfirm';

export const metadata: Metadata = { title: 'Stop alert emails | BioStack', robots: { index: false, follow: false } };

export default async function UnsubscribePage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  return (
    <main className="max-w-lg mx-auto px-4 py-16 space-y-4">
      <h1 className="text-2xl font-semibold text-white">Stop evidence alert emails</h1>
      <UnsubscribeConfirm token={token} />
    </main>
  );
}
