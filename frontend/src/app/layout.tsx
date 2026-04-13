import { AppShell } from '@/components/AppShell';
import { AuthProvider } from '@/lib/AuthProvider';
import { ProfileProvider } from '@/lib/context';
import { SettingsProvider } from '@/lib/settings';
import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'BioStack Mission Control',
  description: 'Protocol intelligence for serious self-experimenters.',
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="h-full antialiased" suppressHydrationWarning>
      <body className="bg-[#0B0F14] text-white/90 font-sans">
        {/* Atmospheric ambient light — gives glass surfaces something to blur through */}
        <div className="pointer-events-none fixed inset-0 overflow-hidden" style={{ zIndex: 0 }}>
          <div className="absolute -top-[20%] right-[5%] w-[70vw] h-[60vh] rounded-full bg-emerald-500/[0.055] blur-[140px]" />
          <div className="absolute top-[40%] -left-[10%] w-[50vw] h-[50vh] rounded-full bg-blue-600/[0.04] blur-[120px]" />
          <div className="absolute -bottom-[10%] right-[20%] w-[45vw] h-[45vh] rounded-full bg-violet-600/[0.035] blur-[110px]" />
        </div>

        <AuthProvider>
          <SettingsProvider>
            <ProfileProvider>
              <AppShell>{children}</AppShell>
            </ProfileProvider>
          </SettingsProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
