'use client';

import { apiClient } from '@/lib/api';
import { useOptionalAuth } from '@/lib/AuthProvider';
import { noticeText, safeAppPath } from '@/lib/notices';
import type { UserNotice } from '@/lib/types';
import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';

/**
 * The header bell: short in-app notes (for example "we found a match for an item you added"), shown the next time the
 * person is signed in. Reading and dismissing never need consent and store nothing new. Renders nothing when signed out.
 */
export function NoticeBell() {
  const auth = useOptionalAuth();
  const signedIn = Boolean(auth?.user);
  const [count, setCount] = useState(0);
  const [open, setOpen] = useState(false);
  const [notices, setNotices] = useState<UserNotice[] | null>(null);
  const [failed, setFailed] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!signedIn) return;
    let live = true;
    const refreshCount = () => {
      // The bell is a convenience; a failed count shows nothing rather than an error.
      Promise.resolve()
        .then(() => apiClient.getUnreadNoticeCount())
        .then((next) => { if (live && typeof next === 'number') setCount(next); })
        .catch(() => undefined);
    };
    refreshCount();
    window.addEventListener('focus', refreshCount);
    return () => { live = false; window.removeEventListener('focus', refreshCount); };
  }, [signedIn]);

  useEffect(() => {
    if (!open) return;
    // Move focus into the popover when it opens so keyboard and screen-reader users land in it, and return
    // focus to the bell when Escape closes it (WCAG 2.4.3 focus order).
    panelRef.current?.focus();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
        buttonRef.current?.focus();
      }
    };
    const onClick = (event: MouseEvent) => {
      if (root.current && !root.current.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onClick);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onClick);
    };
  }, [open]);

  if (!signedIn) return null;

  const toggle = async () => {
    const next = !open;
    setOpen(next);
    if (!next) return;
    try {
      const list = await apiClient.getNotices();
      setNotices(Array.isArray(list) ? list : []);
      setFailed(false);
    } catch {
      setFailed(true);
    }
  };

  const markRead = async (notice: UserNotice) => {
    if (notice.read) return;
    try {
      await apiClient.markNoticeRead(notice.id);
      setNotices((list) => list?.map((n) => (n.id === notice.id ? { ...n, read: true } : n)) ?? null);
      setCount((n) => Math.max(0, n - 1));
    } catch {
      // Leave it unread; it will still be here next time.
    }
  };

  const dismiss = async (notice: UserNotice) => {
    try {
      await apiClient.dismissNotice(notice.id);
      setNotices((list) => list?.filter((n) => n.id !== notice.id) ?? null);
      if (!notice.read) setCount((n) => Math.max(0, n - 1));
    } catch {
      setFailed(true);
    }
  };

  const readAll = async () => {
    try {
      await apiClient.markAllNoticesRead();
      setNotices((list) => list?.map((n) => ({ ...n, read: true })) ?? null);
      setCount(0);
    } catch {
      setFailed(true);
    }
  };

  return (
    <div ref={root} className="relative">
      <button
        ref={buttonRef}
        type="button"
        onClick={() => { void toggle(); }}
        aria-label={count > 0 ? `Notes, ${count} unread` : 'Notes'}
        aria-expanded={open}
        aria-haspopup="dialog"
        className="relative flex h-9 w-9 items-center justify-center rounded-xl border border-white/10 bg-white/[0.03] text-white/50 transition-all hover:text-white active:scale-95 motion-reduce:transition-none motion-reduce:active:scale-100"
      >
        <svg className="h-[18px] w-[18px]" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.8} aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
        </svg>
        {count > 0 && (
          <span data-testid="notice-count" className="absolute -right-1 -top-1 min-w-[16px] rounded-full bg-emerald-500 px-1 text-center text-[10px] font-semibold leading-4 text-black">
            {count > 9 ? '9+' : count}
          </span>
        )}
      </button>
      {open && (
        <div ref={panelRef} role="dialog" tabIndex={-1} aria-label="Notes" className="absolute right-0 z-30 mt-2 w-80 max-w-[90vw] rounded-xl border border-white/10 bg-[#0B0F14] p-3 shadow-xl">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-white">Notes</h2>
            {notices && notices.some((n) => !n.read) && (
              <button type="button" onClick={() => { void readAll(); }} className="text-xs text-white/60 underline">Mark all read</button>
            )}
          </div>
          {failed && <p role="alert" className="mb-2 text-xs text-amber-200">Something went wrong. Please try again.</p>}
          {notices === null && !failed ? (
            <p role="status" className="text-sm text-white/60">Loading…</p>
          ) : notices && notices.length === 0 ? (
            <p role="status" className="text-sm text-white/60">No notes right now.</p>
          ) : (
            <ul className="max-h-80 space-y-2 overflow-y-auto">
              {(notices ?? []).map((notice) => {
                const href = safeAppPath(notice.actionPath);
                return (
                  <li key={notice.id} className={`rounded-lg border p-2 text-sm ${notice.read ? 'border-white/5 text-white/55' : 'border-emerald-400/30 text-white/85'}`}>
                    <p>{noticeText(notice)}</p>
                    <div className="mt-1 flex gap-3 text-xs">
                      {href && (
                        <Link href={href} onClick={() => { void markRead(notice); setOpen(false); }} className="underline">Open</Link>
                      )}
                      {!notice.read && (
                        <button type="button" onClick={() => { void markRead(notice); }} className="text-white/60 underline">Mark read</button>
                      )}
                      <button type="button" onClick={() => { void dismiss(notice); }} aria-label="Dismiss note" className="text-white/60 underline">Dismiss</button>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
