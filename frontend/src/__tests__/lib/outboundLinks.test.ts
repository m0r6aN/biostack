import { readdirSync, readFileSync, statSync } from 'fs';
import { join, relative } from 'path';
import { describe, expect, it } from 'vitest';
import { isAllowlistedSourceUrl } from '@/lib/outboundLinks';

const SRC = join(__dirname, '..', '..');

// Not outbound links a visitor follows: our own domain, schema/XML namespaces, dev hosts,
// form placeholders and server-side API endpoints.
const NON_LINK_HOSTS = new Set(['biostack.cc', 'www.biostack.cc', 'schema.org', 'www.w3.org', 'localhost', 'example.com', 'api.openai.com']);

function files(dir: string): string[] {
  return readdirSync(dir).flatMap(name => {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) return name === '__tests__' ? [] : files(p);
    return /\.(tsx?|mdx?)$/.test(name) ? [p] : [];
  });
}

describe('outbound link allowlist', () => {
  it('accepts research and regulator hosts and rejects vendors and lookalikes', () => {
    expect(isAllowlistedSourceUrl('https://pubmed.ncbi.nlm.nih.gov/1/')).toBe(true);
    expect(isAllowlistedSourceUrl('https://www.fda.gov/x')).toBe(true);
    expect(isAllowlistedSourceUrl('https://doi.org/10.1/x')).toBe(true);
    expect(isAllowlistedSourceUrl('https://www.amazon.com/s?k=x')).toBe(false);
    expect(isAllowlistedSourceUrl('https://fda.gov.example.com/x')).toBe(false);
    expect(isAllowlistedSourceUrl('javascript:alert(1)')).toBe(false);
  });

  it('every hard-coded external link in app, components and lib is allowlisted', () => {
    const offenders: string[] = [];
    for (const dir of ['app', 'components', 'lib']) {
      for (const file of files(join(SRC, dir))) {
        const rel = relative(SRC, file).replace(/\\/g, '/');
        const text = readFileSync(file, 'utf8');
        for (const m of text.matchAll(/https?:\/\/([A-Za-z0-9.-]+)[^\s'"`)<>]*/g)) {
          const host = m[1].toLowerCase();
          if (NON_LINK_HOSTS.has(host) || isAllowlistedSourceUrl(m[0])) continue;
          offenders.push(`${rel}: ${m[0]}`);
        }
      }
    }
    expect(offenders).toEqual([]);
  });
});
