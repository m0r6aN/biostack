import test from 'node:test';
import assert from 'node:assert/strict';
import { runProductionSmoke } from './smoke-prod.mjs';

const vosilasarm = { canonicalName: 'Vosilasarm', slug: 'vosilasarm', aliases: ['Testolone'], mechanismSummary: 'Androgen receptor research.', sourceReferences: ['https://example.org/vosilasarm'] };
const retatrutide = { canonicalName: 'Retatrutide', slug: 'retatrutide', aliases: [], mechanismSummary: 'Multi-receptor agonist research.', sourceReferences: ['https://example.org/retatrutide'] };
function responses(count, { alias = vosilasarm, readinessStatus = 200 } = {}) {
  const compounds = [vosilasarm, retatrutide, ...Array.from({ length: count - 2 }, (_, i) => ({ canonicalName: `Record ${i}`, aliases: [] }))];
  return async url => {
    const path = new URL(url).pathname;
    if (path === '/health/ready') return new Response('Healthy', { status: readinessStatus });
    if (path === '/') return new Response('<html><title>BioStack</title></html>', { headers: { 'Content-Type': 'text/html' } });
    if (path === '/robots.txt') return new Response('User-agent: *\nAllow: /\n');
    const payload = path.endsWith('/testolone') ? alias : path.endsWith('/vosilasarm') ? vosilasarm : path.endsWith('/retatrutide') ? retatrutide : compounds;
    return new Response(JSON.stringify(payload), { headers: { 'Content-Type': 'application/json' } });
  };
}

test('passes all seven checks for the expected corpus and canonical alias lookup', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(208) });
  assert.equal(result.passed, true);
  assert.equal(result.results.length, 7);
});

test('fails corpus drift without skipping the remaining smoke checks', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(209) });
  assert.equal(result.passed, false);
  assert.equal(result.observedActiveRecords, 209);
  assert.match(result.results[1].error, /Expected 208 active records, got 209/);
  assert.equal(result.results.filter(r => r.passed).length, 6);
});

test('fails an unhealthy API and an alias that resolves to the retired name', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(208, { readinessStatus: 503, alias: { ...vosilasarm, canonicalName: 'Testolone' } }) });
  assert.equal(result.passed, false);
  assert.equal(result.results[0].status, 503);
  assert.equal(result.results[4].passed, false);
});
