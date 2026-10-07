import { readFile, readdir, writeFile, mkdir, realpath } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { project, packageInput } from '../frontend/scripts/reviewed-study-design.mjs';

const assert = (ok, message) => { if (!ok) throw new Error(message); };
async function readInside(root, relative) {
  assert(typeof relative === 'string' && !path.isAbsolute(relative), 'Repository-relative input required');
  const resolved = await realpath(path.resolve(root, relative));
  assert(resolved.startsWith(`${await realpath(root)}${path.sep}`), 'Input escapes repository');
  return JSON.parse(await readFile(resolved, 'utf8'));
}
export async function collect(root) {
  const manifest = await readInside(root, 'contracts/reviewed-study-design.json');
  assert(manifest.schemaVersion === '1.0.0' && Object.keys(manifest).sort().join() === 'entries,schemaVersion' && Array.isArray(manifest.entries), 'Invalid display manifest');
  const entries = [];
  for (const item of manifest.entries) {
    assert(Object.keys(item).sort().join() === 'claimId,decisionId,packetPath,payloadPath,receiptPath,sourceItemId', 'Unexpected manifest field');
    const packet = await readInside(root, item.packetPath);
    assert(packet.recordType === 'compound-evidence-packet', 'Expected evidence packet');
    const claims = packet.claims.filter(c => c.claimId === item.claimId);
    const sources = packet.sources.filter(s => s.sourceId === item.sourceItemId);
    assert(claims.length === 1 && sources.length === 1, 'Unique claim/source required');
    const payload = await readInside(root, item.payloadPath);
    assert(packet.compound.canonicalName.trim().toLowerCase() === payload.canonicalName.trim().toLowerCase(), 'Packet compound mismatch');
    entries.push({ payload, claim: claims[0], source: sources[0], observationReceipt: await readInside(root, item.receiptPath), decisionId: item.decisionId });
  }
  const compounds = new Set(entries.map(e => e.payload.canonicalName.trim().toLowerCase()));
  const decisions = [];
  // Discover every current top-level issued batch, not a hand-picked approval list.
  // Later relevant requests/rejections cannot be omitted by the display manifest.
  if (entries.length) {
    const directory = path.join(root, 'research/review-decisions');
    for (const file of (await readdir(directory)).filter(f => f.endsWith('.json')).sort()) {
      const batch = await readInside(root, `research/review-decisions/${file}`);
      if (batch.recordType !== 'review-decision-batch') continue;
      assert(batch.schemaVersion === '1.0.0' && Array.isArray(batch.decisions), 'Unsupported review batch');
      assert(Object.keys(batch).sort().join() === 'batch,decisions,recordType,schemaVersion' && batch.decisions.length > 0, 'Malformed review batch');
      assert(batch.batch && Object.keys(batch.batch).sort().join() === 'batchId,notes,reviewedAt,reviewerId', 'Malformed review batch metadata');
      assert(['batchId', 'reviewerId', 'reviewedAt'].every(k => typeof batch.batch[k] === 'string' && batch.batch[k].trim()) && Number.isFinite(Date.parse(batch.batch.reviewedAt)), 'Invalid review batch metadata');
      assert(Array.isArray(batch.batch.notes) && batch.batch.notes.every(n => typeof n === 'string') && new Set(batch.batch.notes).size === batch.batch.notes.length, 'Invalid review batch notes');
      decisions.push(...batch.decisions.filter(d => compounds.has(d.compoundName?.trim().toLowerCase())));
    }
  }
  return { schemaVersion: '1.0.0', entries, decisions };
}
export async function sync(root, checkOnly) {
  const bundle = await collect(root);
  const output = project(bundle);
  for (const [relative, value] of [
    ['frontend/reviewed-study-design/input.json', packageInput(bundle)],
    ['frontend/src/lib/generated/reviewed-study-design.json', output],
  ]) {
    const target = path.join(root, relative);
    const text = `${JSON.stringify(value, null, 2)}\n`;
    if (checkOnly) assert(await readFile(target, 'utf8') === text, `Stale canonical display package: ${relative}`);
    else { await mkdir(path.dirname(target), { recursive: true }); await writeFile(target, text, 'utf8'); }
  }
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  await sync(path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'), process.argv.includes('--check'));
  console.log('Canonical reviewed study-design package verified.');
}
