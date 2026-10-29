import type { UserNotice } from './types';

/** An in-app path only: one leading slash, no scheme, no protocol-relative or backslash tricks. */
export function safeAppPath(path: string | null | undefined): string | null {
  if (!path || path.length > 200) return null;
  if (!path.startsWith('/') || path.startsWith('//')) return null;
  if (path.includes('\\') || path.includes('://') || /[\u0000-\u001f\u007f]/.test(path)) return null;
  return path;
}

const str = (value: unknown): string => (typeof value === 'string' ? value : '');

/**
 * The text of a note, built here from its key and parameters so the server never ships markup and wording stays in one place.
 * Never includes an amount or a recommendation.
 */
export function noticeText(notice: Pick<UserNotice, 'titleKey' | 'params'>): string {
  if (notice.titleKey === 'notice.catalog_match') {
    const query = str(notice.params.queryName);
    const candidate = str(notice.params.candidateName);
    const lead = notice.params.label === 'same_as' ? 'Same as' : 'Similar to';
    if (query && candidate) return `We found a match for \u201c${query}\u201d: ${lead} ${candidate}. Review it in Everything I\u2019m Taking.`;
  }
  return 'You have a new note.';
}
