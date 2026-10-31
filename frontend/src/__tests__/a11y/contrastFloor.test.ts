import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Static contrast gate. BioStack is a dark UI (#0B0F14 page, cards up to about #1C2024 and tinted panels up to
 * about #0A2328). Text drawn as white at low opacity falls under the WCAG 1.4.3 4.5:1 floor, and jsdom cannot
 * measure colour, so this scans the source for the utility classes that cause it:
 *
 *   - text-white/NN with NN below 50 (white at 50% on the darkest-to-lightest surfaces measures about 5.0:1 or better)
 *   - tinted text at shade 400 to 600 with opacity below 85, or shade 100 to 300 with opacity below 60
 *
 * Variants such as `disabled:` are exempt (WCAG exempts inactive controls). Real measurements come from the
 * Playwright scan documented in docs/INITIATIVES/accessibility/AUDIT.md.
 */

const SRC = resolve(__dirname, '../..');
const SKIP_DIRS = new Set(['__tests__', 'fixtures', 'generated', 'node_modules']);

function sourceFiles(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) return SKIP_DIRS.has(name) ? [] : sourceFiles(path);
    return /\.(tsx|ts)$/.test(name) ? [path] : [];
  });
}

const VARIANTS = '((?:[a-z0-9\\-\\[\\]\\.="\\(\\)&>_~*]+:)*)';
const WHITE = new RegExp(`(?<![\\w-])${VARIANTS}text-white/(\\d{2})(?![\\d\\.\\]])`, 'g');
const TINT = new RegExp(
  `(?<![\\w-])${VARIANTS}text-(rose|red|amber|yellow|orange|emerald|green|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|teal|lime|slate)-(\\d{3})/(\\d{2})(?![\\d\\.\\]])`,
  'g',
);

function findings(): string[] {
  const out: string[] = [];
  for (const file of sourceFiles(SRC)) {
    const text = readFileSync(file, 'utf8');
    text.split('\n').forEach((line, index) => {
      for (const m of line.matchAll(WHITE)) {
        if (m[1].includes('disabled:')) continue;
        if (Number(m[2]) < 50) out.push(`${relative(SRC, file)}:${index + 1}  ${m[0]}  (white text below 50% opacity)`);
      }
      for (const m of line.matchAll(TINT)) {
        if (m[1].includes('disabled:')) continue;
        const shade = Number(m[3]);
        const alpha = Number(m[4]);
        const low = shade >= 400 && shade <= 600 ? alpha < 85 : shade < 400 ? alpha < 60 : false;
        if (low) out.push(`${relative(SRC, file)}:${index + 1}  ${m[0]}  (tinted text too transparent)`);
      }
    });
  }
  return out;
}

describe('contrast floor for text utilities', () => {
  it('has no text colour utilities known to fall below 4.5:1 on the dark surfaces', () => {
    expect(findings(), 'Raise these to text-white/50 or stronger (or drop the opacity on 400-600 shades).').toEqual([]);
  });
});
