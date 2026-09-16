'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useId, useRef, useState } from 'react';
import { apiClient } from '@/lib/api';
import { toSlug } from '@/lib/research/slugs';
import type { KnowledgeEntry } from '@/lib/types';

const MAX_RESULTS = 8;

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  const tag = target.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || target.isContentEditable;
}

/**
 * Compact, always-available way to find a compound in the public library
 * without adding it — the library is the primary destination, not a
 * secondary tab, so this ships in the app shell header rather than being
 * buried on a single page. Filters the same knowledge list the /knowledge
 * index already fetches (no new endpoint), client-side, by name, alias and
 * classification.
 */
export function GlobalSearch() {
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();
  const inputId = `${listboxId}-input`;

  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [entries, setEntries] = useState<KnowledgeEntry[] | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  const loadingRef = useRef(false);

  const ensureEntries = useCallback(() => {
    if (entries !== null || loadingRef.current) return;
    loadingRef.current = true;
    apiClient
      .getAllKnowledgeCompounds()
      .then(data => setEntries(data))
      .catch(() => {
        setLoadFailed(true);
        setEntries([]);
      })
      .finally(() => {
        loadingRef.current = false;
      });
  }, [entries]);

  // "/" focuses search, but only when the visitor isn't already typing
  // somewhere else on the page.
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (
        event.key === '/' &&
        !event.metaKey &&
        !event.ctrlKey &&
        !event.altKey &&
        !isTypingTarget(event.target)
      ) {
        event.preventDefault();
        ensureEntries();
        inputRef.current?.focus();
      }
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [ensureEntries]);

  const normalizedQuery = query.trim().toLowerCase();
  const results =
    normalizedQuery && entries
      ? entries
          .filter(
            entry =>
              entry.canonicalName.toLowerCase().includes(normalizedQuery) ||
              entry.aliases.some(alias => alias.toLowerCase().includes(normalizedQuery)) ||
              entry.classification.toLowerCase().includes(normalizedQuery)
          )
          .slice(0, MAX_RESULTS)
      : [];

  const goToEntry = (entry: KnowledgeEntry) => {
    setOpen(false);
    setQuery('');
    router.push(`/knowledge/${toSlug(entry.canonicalName)}`);
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault();
      if (results[0]) goToEntry(results[0]);
      return;
    }
    if (event.key === 'Escape') {
      if (query) {
        setQuery('');
      } else {
        inputRef.current?.blur();
      }
      setOpen(false);
    }
  };

  const handleBlur = (event: React.FocusEvent<HTMLDivElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setOpen(false);
    }
  };

  const showResults = open && normalizedQuery.length > 0;

  return (
    <div ref={containerRef} onBlur={handleBlur} className="relative w-full max-w-[220px] sm:max-w-xs motion-reduce:transition-none">
      <label htmlFor={inputId} className="sr-only">
        Search the library
      </label>
      <div className="relative">
        <svg
          aria-hidden="true"
          className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-white/25"
          fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          id={inputId}
          ref={inputRef}
          type="text"
          role="combobox"
          aria-expanded={showResults}
          aria-controls={listboxId}
          aria-autocomplete="list"
          value={query}
          onFocus={() => {
            ensureEntries();
            setOpen(true);
          }}
          onChange={event => {
            ensureEntries();
            setQuery(event.target.value);
            setOpen(true);
          }}
          onKeyDown={handleKeyDown}
          placeholder="Search the library…"
          className="w-full h-9 pl-8 pr-3 rounded-lg border border-white/10 bg-white/[0.04] text-[13px] text-white placeholder:text-white/30 focus:outline-none focus:border-emerald-500/40 focus:bg-white/[0.06] transition-colors motion-reduce:transition-none"
        />
      </div>

      {showResults && (
        <ul
          id={listboxId}
          role="listbox"
          aria-label="Library search results"
          className="absolute right-0 z-30 mt-1.5 w-72 max-w-[80vw] max-h-80 overflow-y-auto rounded-xl border border-white/10 bg-[#121923] py-1 shadow-[0_12px_32px_rgba(0,0,0,0.4)]"
        >
          {results.length > 0 ? (
            results.map(entry => (
              <li key={entry.canonicalName} role="option" aria-selected="false">
                <Link
                  href={`/knowledge/${toSlug(entry.canonicalName)}`}
                  onClick={() => {
                    setOpen(false);
                    setQuery('');
                  }}
                  className="flex items-center justify-between gap-3 px-3 py-2 text-[13px] text-white/80 hover:bg-white/5 focus-visible:outline-none focus-visible:bg-white/5"
                >
                  <span className="truncate">{entry.canonicalName}</span>
                  <span className="shrink-0 rounded-full border border-white/10 bg-white/5 px-2 py-0.5 text-[10px] font-medium text-white/45">
                    {entry.classification}
                  </span>
                </Link>
              </li>
            ))
          ) : (
            <li role="option" aria-disabled="true" className="px-3 py-2 text-[13px] text-white/40">
              {loadFailed ? 'Search is unavailable right now.' : `No matches for "${query.trim()}"`}
            </li>
          )}
        </ul>
      )}
    </div>
  );
}
