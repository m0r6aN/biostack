import { noticeText, safeAppPath } from '@/lib/notices';
import { describe, expect, it } from 'vitest';

describe('safeAppPath', () => {
  it.each(['/my-protocol?tab=taking', '/knowledge/creatine', '/'])('keeps the in-app path %s', (path) => {
    expect(safeAppPath(path)).toBe(path);
  });

  it.each([null, undefined, '', 'my-protocol', '//evil.example', 'https://evil.example', 'javascript:alert(1)', '/a\\b', '/x://y', '/a\u0000b', `/${'a'.repeat(200)}`])(
    'refuses %s',
    (path) => {
      expect(safeAppPath(path as string | null | undefined)).toBeNull();
    },
  );
});

describe('noticeText', () => {
  it('words a catalog match from its parameters, with the same Same as / Similar to labels as the offers', () => {
    const same = noticeText({ titleKey: 'notice.catalog_match', params: { queryName: 'Ozempic', candidateName: 'Semaglutide', label: 'same_as' } });
    expect(same).toBe('We found a match for \u201cOzempic\u201d: Same as Semaglutide. Review it in Everything I\u2019m Taking.');
    expect(noticeText({ titleKey: 'notice.catalog_match', params: { queryName: 'cretine', candidateName: 'Creatine', label: 'similar' } })).toContain('Similar to Creatine');
  });

  it('falls back to a plain line for an unknown key or missing parameters, and never echoes markup', () => {
    expect(noticeText({ titleKey: 'notice.future_thing', params: { queryName: '<b>x</b>' } })).toBe('You have a new note.');
    expect(noticeText({ titleKey: 'notice.catalog_match', params: {} })).toBe('You have a new note.');
  });
});
