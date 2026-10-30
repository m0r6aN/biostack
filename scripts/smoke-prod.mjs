#!/usr/bin/env node
import { writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

// The public list is every non-retired record: the curated seed plus the drafts that the daily
// open-discovery job (kw-discovery) adds, so the count grows by design. 208 on 2026-10-26 was
// 158 curated records plus the first 50 drafts; 237 on 2026-10-28 and 245 on 2026-10-30 are the
// same list after later discovery runs. A hard count therefore fails on healthy production.
// The smoke asserts a rule instead: never fewer than the curated baseline, never an absurd
// number (runaway duplicates), and the curated sentinel records are present.
// Raise MIN_ACTIVE_RECORDS when curated records are added; lower it only for a deliberate retirement.
export const MIN_ACTIVE_RECORDS = 158;
export const MAX_ACTIVE_RECORDS = 1000;
// Curated records that must stay separate or present (creatine and creatine monohydrate stay separate by ruling D-D).
export const SENTINEL_RECORDS = ['Semaglutide', 'Retatrutide', 'Vosilasarm', 'Creatine', 'Creatine Monohydrate'];

function requireCompound(value, canonicalName) {
  if (value?.canonicalName !== canonicalName) throw new Error(`Expected ${canonicalName}, got ${value?.canonicalName ?? 'no canonical name'}`);
  if (typeof value.mechanismSummary !== 'string' || !value.mechanismSummary.trim()) throw new Error(`${canonicalName} has no mechanism summary`);
  if (!Array.isArray(value.sourceReferences) || value.sourceReferences.length === 0) throw new Error(`${canonicalName} has no source references`);
}

export async function runProductionSmoke({
  apiOrigin = 'https://api.biostack.cc',
  webOrigin = 'https://biostack.cc',
  fetchImpl = fetch,
  timeoutMs = 20_000,
} = {}) {
  const results = [];
  let compounds;
  async function check(name, origin, path, validate) {
    const url = new URL(path, origin).href;
    const started = performance.now();
    let status;
    try {
      const response = await fetchImpl(url, {
        method: 'GET', redirect: 'manual', credentials: 'omit',
        headers: { 'Cache-Control': 'no-cache' }, signal: AbortSignal.timeout(timeoutMs),
      });
      status = response.status;
      if (status !== 200) throw new Error(`Expected HTTP 200, got ${status}`);
      const text = await response.text();
      const details = validate(text, response.headers);
      results.push({ name, url, status, passed: true, details, elapsedMs: Math.round(performance.now() - started) });
    } catch (error) {
      results.push({ name, url, status, passed: false, error: error.message, elapsedMs: Math.round(performance.now() - started) });
    }
  }
  function json(text, headers) {
    if (!headers.get('content-type')?.includes('application/json')) throw new Error('Expected a JSON response');
    return JSON.parse(text);
  }

  await check('API readiness', apiOrigin, '/health/ready', text => {
    if (text.trim() !== 'Healthy') throw new Error('Readiness did not report Healthy');
    return { healthy: true };
  });
  await check('Active compound list', apiOrigin, '/api/v1/knowledge/compounds', (text, headers) => {
    compounds = json(text, headers);
    if (!Array.isArray(compounds)) throw new Error('Expected an array of active compounds');
    if (compounds.some(c => typeof c.canonicalName !== 'string' || !c.canonicalName.trim())) throw new Error('Compound list contains an unnamed record');
    if (new Set(compounds.map(c => c.canonicalName.toLowerCase())).size !== compounds.length) throw new Error('Compound list contains duplicate names');
    if (compounds.length < MIN_ACTIVE_RECORDS) throw new Error(`Expected at least ${MIN_ACTIVE_RECORDS} active records, got ${compounds.length}`);
    if (compounds.length > MAX_ACTIVE_RECORDS) throw new Error(`Expected at most ${MAX_ACTIVE_RECORDS} active records, got ${compounds.length}`);
    const names = new Set(compounds.map(c => c.canonicalName.toLowerCase()));
    const missing = SENTINEL_RECORDS.filter(n => !names.has(n.toLowerCase()));
    if (missing.length) throw new Error(`Compound list is missing curated records: ${missing.join(', ')}`);
    return { activeRecords: compounds.length };
  });
  for (const [name, slug] of [['Vosilasarm', 'vosilasarm'], ['Retatrutide', 'retatrutide']]) {
    await check(name, apiOrigin, `/api/v1/knowledge/compounds/${slug}`, (text, headers) => {
      const compound = json(text, headers);
      requireCompound(compound, name);
      if (compound.slug !== slug) throw new Error(`Expected slug ${slug}`);
      return { canonicalName: compound.canonicalName, sources: compound.sourceReferences.length };
    });
  }
  await check('Testolone alias search', apiOrigin, '/api/v1/knowledge/compounds/testolone', (text, headers) => {
    const compound = json(text, headers);
    requireCompound(compound, 'Vosilasarm');
    if (compound.slug !== 'vosilasarm') throw new Error('Testolone did not resolve to the Vosilasarm slug');
    if (!Array.isArray(compounds)) throw new Error('No compound list available to verify alias search');
    const matches = compounds.filter(c => c.canonicalName.toLowerCase().includes('testolone') || c.aliases?.some(a => a.toLowerCase().includes('testolone')));
    if (matches.length !== 1 || matches[0].canonicalName !== 'Vosilasarm') throw new Error('List search for testolone does not uniquely resolve to Vosilasarm');
    return { resolvesTo: compound.canonicalName, listSearchMatches: matches.length };
  });
  await check('Web homepage', webOrigin, '/', (text, headers) => {
    if (!headers.get('content-type')?.includes('text/html') || !/<html\b/i.test(text) || !/BioStack/i.test(text)) throw new Error('Expected BioStack HTML homepage');
    return { bioStackHtml: true };
  });
  await check('Web robots.txt', webOrigin, '/robots.txt', text => {
    if (!/^User-agent:\s*\S+/im.test(text) || !/^(Allow|Disallow|Sitemap):/im.test(text)) throw new Error('Expected robots directives');
    return { robotsDirectives: true };
  });
  return {
    checkedAtUtc: new Date().toISOString(), apiOrigin, webOrigin,
    minimumActiveRecords: MIN_ACTIVE_RECORDS,
    observedActiveRecords: Array.isArray(compounds) ? compounds.length : null,
    passed: results.every(r => r.passed), results,
  };
}

async function main(args) {
  const values = {};
  for (let i = 0; i < args.length; i += 2) {
    if (!['--api-origin', '--web-origin', '--output'].includes(args[i]) || !args[i + 1]) throw new Error('Usage: node scripts/smoke-prod.mjs [--api-origin URL] [--web-origin URL] [--output FILE]');
    values[args[i]] = args[i + 1];
  }
  const result = await runProductionSmoke({ apiOrigin: values['--api-origin'], webOrigin: values['--web-origin'] });
  if (values['--output']) await writeFile(resolve(values['--output']), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  main(process.argv.slice(2)).catch(error => { console.error(error.message); process.exitCode = 1; });
}
