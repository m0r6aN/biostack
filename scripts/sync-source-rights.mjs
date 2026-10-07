import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Generated copies keep each Docker build context self-contained. The shared file
// is authoritative; CI rejects missing or stale copies before tests/build/publish.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = 'shared/source-rights/drugbank-containment.v1.json';
const targets = [
  'backend/src/BioStack.KnowledgeWorker/SourceRights/drugbank-containment.v1.json',
  'frontend/src/lib/research/source-rights/drugbank-containment.v1.json',
];
const canonical = JSON.parse(await readFile(path.join(root, source), 'utf8'));
if (canonical.schemaVersion !== '1.0.0' || canonical.decisionId !== 'C1'
    || canonical.sourceClassId !== 'drugbank' || !canonical.sourceIds?.length || !canonical.hostSuffixes?.length) {
  throw new Error('Invalid C1 containment manifest; review the policy contract before changing it.');
}
const normalized = `${JSON.stringify(canonical, null, 2)}\n`;
const checkOnly = process.argv.includes('--check');
for (const relativePath of targets) {
  const target = path.join(root, relativePath);
  if (checkOnly) {
    // Git line-ending conversion must not make Windows and Linux disagree.
    const actual = (await readFile(target, 'utf8')).replace(/\r\n/g, '\n');
    if (actual !== normalized) throw new Error(`${relativePath} is out of sync with ${source}. Run node scripts/sync-source-rights.mjs.`);
  } else {
    await mkdir(path.dirname(target), { recursive: true });
    await writeFile(target, normalized, 'utf8');
  }
}
console.log(checkOnly ? 'Source-rights copies are current.' : 'Source-rights copies updated.');
