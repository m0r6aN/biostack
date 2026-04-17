'use client';

import { trackLandingPathSelection, type LandingPath } from '@/lib/landingAnalytics';
import Link from 'next/link';

interface LandingPathCardProps {
  action: string;
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
  action,
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
      className={`group relative flex min-h-[132px] items-stretch justify-between gap-4 overflow-hidden rounded-lg border px-3.5 py-3.5 shadow-[0_12px_34px_rgba(0,0,0,0.2)] transition duration-200 hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2 sm:min-h-[136px] sm:px-4 lg:min-h-[154px] ${cardClassName}`}
    >
      <span aria-hidden="true" className={`absolute inset-y-3 left-0 w-1 rounded-r-full ${railClassName}`} />
      <span className="flex min-w-0 flex-1 flex-col pl-1.5">
        <span className="block text-[11px] font-semibold uppercase tracking-[0.18em] text-white/46">
          {label}
        </span>
        <span className="mt-1 block text-lg font-semibold tracking-tight text-white sm:text-xl">{title}</span>
        <span className="mt-1 block text-[13px] leading-5 text-white/64 transition-colors group-hover:text-white/76 sm:text-sm">
          {body}
        </span>
        <span className="mt-auto flex items-center justify-between gap-3 pt-3">
          <span className={`block text-[13px] font-semibold sm:text-sm ${signalClassName}`}>{signal}</span>
          <span className="text-sm font-semibold text-white/70 transition-colors group-hover:text-white">
            {action} &gt;
          </span>
        </span>
      </span>
      <span
        aria-hidden="true"
        className={`hidden h-11 w-11 shrink-0 items-center justify-center rounded-lg border bg-black/20 text-lg transition sm:flex ${iconClassName}`}
      >
        &gt;
      </span>
    </Link>
  );
}
