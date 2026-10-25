import type { Metadata } from 'next';
import { AlertSettings } from '@/components/alerts/AlertSettings';

export const metadata: Metadata = { title: 'Evidence alerts | BioStack', robots: { index: false, follow: false } };

export default function AlertsPage() {
  return (
    <main className="max-w-2xl mx-auto px-4 py-10 space-y-6">
      <h1 className="text-2xl font-semibold text-white">Evidence alerts</h1>
      <AlertSettings />
    </main>
  );
}
