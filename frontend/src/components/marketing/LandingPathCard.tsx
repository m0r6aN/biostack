'use client';

import { trackLandingPathSelection, type LandingPath } from '@/lib/landingAnalytics';
import Link from 'next/link';

interface LandingPathCardProps {
  body: string;
  cardClassName: string;
  href: string;
  iconClassName: string;
  label: string;
  path: LandingPath;
  railClassName: string;
  signal: string;
  signalClassName: string;
  title: string;
}

export function LandingPathCard({
  body,
  cardClassName,
  href,
  iconClassName,
  label,
  path,
  railClassName,
  signal,
  signalClassName,
  title,
}: LandingPathCardProps) {
  return (
    <Link
      href={href}
      onClick={() => trackLandingPathSelection(path)}
      className={`group relative flex min-h-[96px] items-center justify-between gap-4 overflow-hidden rounded-lg border px-3.5 py-3 shadow-[0_12px_34px_rgba(0,0,0,0.2)] transition duration-200 hover:-translate-y-0.5 hover:shadow-[0_0_24px_rgba(255,255,255,0.06)] focus-visible:outline-none focus-visible:ring-2 sm:min-h-[106px] sm:px-4 ${cardClassName}`}
    >
      <span aria-hidden="true" className={`absolute inset-y-3 left-0 w-1 rounded-r-full ${railClassName}`} />
      <span className="min-w-0 pl-1.5">
        <span className="block text-[11px] font-semibold uppercase tracking-[0.18em] text-white/46">
          {label}
        </span>
        <span className="mt-1 block text-lg font-semibold tracking-tight text-white sm:text-xl">{title}</span>
        <span className="mt-0.5 block text-[13px] leading-5 text-white/64 sm:text-sm">{body}</span>
        <span className={`mt-1.5 block text-[13px] font-semibold sm:text-sm ${signalClassName}`}>{signal}</span>
      </span>
      <span
        aria-hidden="true"
        className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg border bg-black/20 text-lg transition ${iconClassName}`}
      >
        &gt;
      </span>
    </Link>
  );
}
