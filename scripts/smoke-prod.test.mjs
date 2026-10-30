import test from 'node:test';
import assert from 'node:assert/strict';
import { runProductionSmoke, MIN_ACTIVE_RECORDS, MAX_ACTIVE_RECORDS } from './smoke-prod.mjs';

const vosilasarm = { canonicalName: 'Vosilasarm', slug: 'vosilasarm', aliases: ['Testolone'], mechanismSummary: 'Androgen receptor research.', sourceReferences: ['https://example.org/vosilasarm'] };
const retatrutide = { canonicalName: 'Retatrutide', slug: 'retatrutide', aliases: [], mechanismSummary: 'Multi-receptor agonist research.', sourceReferences: ['https://example.org/retatrutide'] };
const sentinels = ['Semaglutide', 'Creatine', 'Creatine monohydrate'].map(canonicalName => ({ canonicalName, aliases: [] }));
function responses(count, { alias = vosilasarm, readinessStatus = 200, omitSentinels = false } = {}) {
  const fixed = [vosilasarm, retatrutide, ...(omitSentinels ? [] : sentinels)];
  const compounds = [...fixed, ...Array.from({ length: Math.max(0, count - fixed.length) }, (_, i) => ({ canonicalName: `Record ${i}`, aliases: [] }))];
  return async url => {
    const path = new URL(url).pathname;
    if (path === '/health/ready') return new Response('Healthy', { status: readinessStatus });
    if (path === '/') return new Response('<html><title>BioStack</title></html>', { headers: { 'Content-Type': 'text/html' } });
    if (path === '/robots.txt') return new Response('User-agent: *\nAllow: /\n');
    const payload = path.endsWith('/testolone') ? alias : path.endsWith('/vosilasarm') ? vosilasarm : path.endsWith('/retatrutide') ? retatrutide : compounds;
    return new Response(JSON.stringify(payload), { headers: { 'Content-Type': 'application/json' } });
  };
}

test('passes all seven checks at the curated baseline and as discovery drafts grow the list', async () => {
  for (const count of [158, 208, 237, 245]) {
    const result = await runProductionSmoke({ fetchImpl: responses(count) });
    assert.equal(result.passed, true, `count ${count}`);
    assert.equal(result.observedActiveRecords, count);
    assert.equal(result.results.length, 7);
  }
});

test('fails a list below the curated baseline without skipping the remaining checks', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(MIN_ACTIVE_RECORDS - 1) });
  assert.equal(result.passed, false);
  assert.match(result.results[1].error, /at least 158 active records, got 157/);
  assert.equal(result.results.filter(r => r.passed).length, 6);
});

test('fails an absurd list size (runaway duplicates or a bad import)', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(MAX_ACTIVE_RECORDS + 1) });
  assert.equal(result.passed, false);
  assert.match(result.results[1].error, /at most 1000 active records/);
});

test('fails when a curated sentinel record is missing', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(208, { omitSentinels: true }) });
  assert.equal(result.passed, false);
  assert.match(result.results[1].error, /missing curated records: Semaglutide, Creatine, Creatine Monohydrate/);
});

test('fails an unhealthy API and an alias that resolves to the retired name', async () => {
  const result = await runProductionSmoke({ fetchImpl: responses(208, { readinessStatus: 503, alias: { ...vosilasarm, canonicalName: 'Testolone' } }) });
  assert.equal(result.passed, false);
  assert.equal(result.results[0].status, 503);
  assert.equal(result.results[4].passed, false);
});
