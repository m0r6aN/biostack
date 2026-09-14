import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, cp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { hash, project, packageInput, checkOrWrite } from './reviewed-study-design.mjs';

const now = new Date('2026-09-14T00:00:00Z');
function fixture() {
  const payload = {
    canonicalName: 'Synthetic compound', kind: 'dated-review-study-design', claimId: 'synthetic-design',
    statement: 'A synthetic review included three studies.',
    citation: { displayLabel: 'Synthetic et al., 2025', sourceItemId: 'synthetic-paper', pmid: '12345', doi: '10.1234/test', url: 'https://pubmed.ncbi.nlm.nih.gov/12345/' },
    searchCutoff: '2024-06-03', languageScope: 'English-language literature', limitations: 'Synthetic fixture only; no clinical conclusion.',
  };
  const claim = { claimId: payload.claimId, claimType: 'evidence-gap', statement: payload.statement, sourceRefs: ['synthetic-paper'], context: { useCase: 'review evidence' }, reviewFlags: ['HISTORICAL SAFETY TEXT MUST NEVER LEAK'] };
  const source = { sourceId: 'synthetic-paper', pmid: '12345', doi: '10.1234/test', url: payload.citation.url };
  const observationReceipt = { author: 'synthetic observer', httpStatus: 200, responseSha256: 'a'.repeat(64) };
  const binding = { surface: 'knowledge-card-study-design', claimId: claim.claimId, claimSha256: hash(claim), sourceItemId: source.sourceId, sourceRecordId: source.pmid, sourceSnapshotSha256: hash(source), observationReceiptSha256: hash(observationReceipt), payloadSha256: hash(payload) };
  const decision = { decisionId: 'synthetic-approval', compoundName: payload.canonicalName, decision: 'approve-claims', reviewerId: 'synthetic independent reviewer', reviewedAt: '2026-09-13T00:00:00Z', scope: { claimIds: [claim.claimId], qualityFlags: [], reviewCategories: [], promotionBlockers: ['Broader compound remains pending'], displayBindings: [binding] }, notes: ['Synthetic fixture, not issued'], clearsSoftPromotionBlockers: false, expiresAt: null };
  return { schemaVersion: '1.0.0', entries: [{ payload, claim, source, observationReceipt, decisionId: decision.decisionId }], decisions: [decision] };
}
test('exact approval exports only accepted explanation, with no historical flags or compound promotion', () => {
  const bundle = fixture(); const out = project(bundle, now);
  assert.equal(out.length, 1); assert.equal(out[0].citation.pmid, '12345');
  assert.equal(out[0].acceptanceDecisionId, 'synthetic-approval');
  assert.ok(!JSON.stringify(out).includes('HISTORICAL')); assert.ok(!JSON.stringify(out).includes('sourceRefs'));
  assert.equal(bundle.decisions[0].clearsSoftPromotionBlockers, false);
});
for (const [name, mutate] of [
  ['absent decision', b => { b.decisions = []; }],
  ['plain approve-claims', b => { delete b.decisions[0].scope.displayBindings; }],
  ['whole compound approval', b => { b.decisions[0].decision = 'approve-for-promotion'; }],
  ['pending decision', b => { b.decisions[0].decision = 'request-changes'; }],
  ['claim context change', b => { b.entries[0].claim.context.useCase = 'new context'; }],
  ['statement change', b => { b.entries[0].payload.statement = 'Another statement'; }],
  ['source DOI change', b => { b.entries[0].source.doi = '10.1234/other'; }],
  ['receipt change', b => { b.entries[0].observationReceipt.author = 'other'; }],
  ['unknown clinical field even with recomputed payload hash', b => { b.entries[0].payload.dose = 'hidden'; b.decisions[0].scope.displayBindings[0].payloadSha256 = hash(b.entries[0].payload); }],
  ['wrong compound', b => { b.decisions[0].compoundName = 'Other compound'; }],
  ['wrong PMID', b => { b.decisions[0].scope.displayBindings[0].sourceRecordId = '67890'; }],
  ['wildcard claim', b => { b.decisions[0].scope.claimIds = ['*']; }],
  ['unknown binding property', b => { b.decisions[0].scope.displayBindings[0].approved = true; }],
  ['expired approval', b => { b.decisions[0].expiresAt = now.toISOString(); }],
  ['future approval', b => { b.decisions[0].reviewedAt = '2099-01-01T00:00:00Z'; }],
  ['invalid calendar date', b => { b.decisions[0].reviewedAt = '2026-02-30T00:00:00Z'; }],
  ['schema-invalid flag type', b => { b.decisions[0].clearsSoftPromotionBlockers = 'false'; }],
  ['duplicate decision', b => { b.decisions.push(structuredClone(b.decisions[0])); }],
  ['duplicate compound payload', b => { b.entries.push(structuredClone(b.entries[0])); }],
  ['unsupported input version', b => { b.schemaVersion = '2.0.0'; }],
]) test(`rejects ${name}`, () => { const b = fixture(); mutate(b); assert.throws(() => project(b, now)); });

for (const kind of ['request-changes', 'reject', 'archive-draft']) {
  for (const scope of [[], ['synthetic-design']]) test(`later ${kind} with ${scope.length ? 'exact' : 'compound-wide'} scope blocks`, () => {
    const b = fixture(); const d = structuredClone(b.decisions[0]);
    d.decisionId = 'later'; d.decision = kind; d.reviewedAt = now.toISOString(); d.scope.claimIds = scope; delete d.scope.displayBindings;
    b.decisions.push(d); assert.throws(() => project(b, now), /blocks display/);
  });
}
test('older and later sibling requests stay unresolved without blocking an exact later acceptance', () => {
  const b = fixture(); const d = structuredClone(b.decisions[0]);
  d.decisionId = 'sibling'; d.decision = 'request-changes'; d.reviewedAt = now.toISOString(); d.scope.claimIds = ['other-claim']; delete d.scope.displayBindings;
  b.decisions.push(d); const old = structuredClone(d); old.decisionId = 'older'; old.reviewedAt = '2026-01-01T00:00:00Z'; old.scope.claimIds = []; b.decisions.push(old);
  assert.equal(project(b, now).length, 1); assert.equal(b.decisions[1].decision, 'request-changes');
});
test('empty real inputs do not publish anything', () => assert.deepEqual(project({ schemaVersion: '1.0.0', entries: [], decisions: [] }, now), []));

test('packaged input omits historical flags, context, receipt text and decision notes while retaining enforcement', () => {
  const b = fixture(); const packed = packageInput(b, now);
  const text = JSON.stringify(packed);
  for (const excluded of ['HISTORICAL', 'reviewFlags', 'useCase', 'synthetic observer', 'Broader compound remains pending', 'Synthetic fixture, not issued']) assert.ok(!text.includes(excluded));
  assert.deepEqual(project(packed, now, true), project(b, now));
  packed.entries[0].evidenceHashes.claimSha256 = '0'.repeat(64);
  assert.throws(() => project(packed, now, true), /claimSha256 mismatch/);
});
test('root refuses changed canonical history before minimal packaging', () => {
  const b = fixture(); b.entries[0].claim.reviewFlags.push('unreviewed change');
  assert.throws(() => packageInput(b, now), /claimSha256 mismatch/);
});
test('packaged expiry and later relevant decision still block', () => {
  const b = packageInput(fixture(), now);
  b.decisions[0].expiresAt = now.toISOString();
  assert.throws(() => project(b, now, true), /Expired/);
  b.decisions[0].expiresAt = null;
  b.decisions.push({ ...b.decisions[0], decisionId: 'later', decision: 'request-changes', reviewedAt: now.toISOString(), scope: { claimIds: [], qualityFlags: [], reviewCategories: [], promotionBlockers: [] } });
  assert.throws(() => project(b, now, true), /blocks display/);
});
test('frontend-only Docker context checks without root research or dependencies, rejects stale artifact', async () => {
  const root = await mkdtemp(path.join(tmpdir(), 'biostack-display-'));
  try {
    await mkdir(path.join(root, 'scripts')); await mkdir(path.join(root, 'reviewed-study-design'));
    await mkdir(path.join(root, 'src/lib/generated'), { recursive: true });
    await cp(new URL('./reviewed-study-design.mjs', import.meta.url), path.join(root, 'scripts/reviewed-study-design.mjs'));
    await writeFile(path.join(root, 'reviewed-study-design/input.json'), JSON.stringify({ schemaVersion: '1.0.0', entries: [], decisions: [] }));
    await writeFile(path.join(root, 'src/lib/generated/reviewed-study-design.json'), '[]\n');
    assert.equal(spawnSync(process.execPath, ['scripts/reviewed-study-design.mjs', '--check'], { cwd: root }).status, 0);
    await writeFile(path.join(root, 'src/lib/generated/reviewed-study-design.json'), '[{"stale":true}]\n');
    assert.notEqual(spawnSync(process.execPath, ['scripts/reviewed-study-design.mjs', '--check'], { cwd: root }).status, 0);
    await assert.rejects(checkOrWrite(root), /Stale/);
    assert.match(await readFile(path.join(root, 'src/lib/generated/reviewed-study-design.json'), 'utf8'), /stale/);
  } finally { await rm(root, { recursive: true, force: true }); }
});
