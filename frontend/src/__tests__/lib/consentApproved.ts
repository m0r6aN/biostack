import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { expect } from 'vitest';

// The approved document is the source of truth: Clint signed off on it 2026-10-28 with "no edits".
export const APPROVED_PATH = resolve(process.cwd(), '../docs/INITIATIVES/consent-v2/consent-v2-approved.md');

export const plain = (value: string) =>
  value.replace(/\*\*/g, '').replace(/(^|[^\w])\*([^*]+)\*/g, '$1$2').replace(/\s+/g, ' ').trim();

/** The section 2 text of the approved document, split by its "###" subsections. */
export function approvedSections(): Record<string, string[]> {
  const doc = readFileSync(APPROVED_PATH, 'utf8').replace(/\r\n/g, '\n');
  const start = doc.indexOf('## 2. Proposed v2 text');
  const end = doc.indexOf('\n## 3.', start);
  expect(start).toBeGreaterThan(-1);
  expect(end).toBeGreaterThan(start);
  const sections: Record<string, string[]> = {};
  let current = '';
  for (const line of doc.slice(start, end).split('\n')) {
    if (line.startsWith('### ')) {
      current = line.slice(4).trim();
      sections[current] = [];
    } else if (current && line.trim() && !line.startsWith('---') && !line.startsWith('Footer:') && !line.startsWith('Buttons:')) {
      sections[current].push(line);
    }
  }
  return sections;
}
