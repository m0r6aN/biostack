import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const canonicalPath = path.join(root, 'contracts', 'product-contract.v1.json');
const targetPaths = [
  path.join(root, 'backend', 'src', 'BioStack.Application', 'ProductContract', 'product-contract.v1.json'),
  path.join(root, 'frontend', 'src', 'contracts', 'product-contract.v1.json'),
];

const canonical = JSON.parse(await readFile(canonicalPath, 'utf8'));
const normalized = `${JSON.stringify(canonical, null, 2)}\n`;
const checkOnly = process.argv.includes('--check');

if (canonical.contractVersion !== '1.0.0') {
  throw new Error('Update the versioned filename and dependent loaders before changing contractVersion.');
}

if (canonical.billing.interval !== 'month' || canonical.billing.pastDueGraceDays !== 0) {
  throw new Error('The launch contract must remain monthly-only with immediate past-due downgrade unless formally amended.');
}

for (const targetPath of targetPaths) {
  if (checkOnly) {
    const target = await readFile(targetPath, 'utf8');
    if (target !== normalized) {
      throw new Error(`${path.relative(root, targetPath)} is out of sync with ${path.relative(root, canonicalPath)}.`);
    }
    continue;
  }

  await mkdir(path.dirname(targetPath), { recursive: true });
  await writeFile(targetPath, normalized, 'utf8');
}

console.log(checkOnly ? 'Product contract mirrors are current.' : 'Product contract mirrors updated.');
