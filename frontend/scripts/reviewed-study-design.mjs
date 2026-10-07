import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Hash encoding: recursively sorted object keys, original array order, UTF-8 JSON,
// no trailing newline. This hashes the full claim/source/receipt/payload objects.
export function canonical(value) {
  if (Array.isArray(value)) return `[${value.map(canonical).join(',')}]`;
  if (value && typeof value === 'object') return `{${Object.keys(value).sort().map(k => `${JSON.stringify(k)}:${canonical(value[k])}`).join(',')}}`;
  return JSON.stringify(value);
}
export const hash = value => createHash('sha256').update(canonical(value), 'utf8').digest('hex');
const requireThat = (condition, reason) => { if (!condition) throw new Error(reason); };
const normalized = value => value.trim().toLowerCase();
function fields(value, keys, label) {
  requireThat(value && typeof value === 'object' && !Array.isArray(value), `${label}: object required`);
  requireThat(Object.keys(value).every(k => keys.includes(k)) && keys.every(k => Object.hasOwn(value, k)), `${label}: unexpected or missing field`);
}
function string(value, label) {
  requireThat(typeof value === 'string' && value.trim().length > 0 && !value.includes('*'), `${label}: explicit nonempty value required`);
}
function date(value, label) {
  requireThat(typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/.test(value) && Number.isFinite(Date.parse(value)), `${label}: timestamp required`);
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  requireThat(new Date(Date.UTC(year, month - 1, day)).toISOString().slice(0, 10) === value.slice(0, 10), `${label}: invalid calendar date`);
  return Date.parse(value);
}
const bindingFields = ['surface', 'claimId', 'claimSha256', 'sourceItemId', 'sourceRecordId', 'sourceSnapshotSha256', 'observationReceiptSha256', 'payloadSha256'];
function validateBinding(binding) {
  fields(binding, bindingFields, 'display binding');
  requireThat(binding.surface === 'knowledge-card-study-design', 'Unsupported display surface');
  for (const key of bindingFields.filter(k => k.endsWith('Sha256'))) requireThat(/^[a-f0-9]{64}$/.test(binding[key]), `Invalid ${key}`);
  for (const key of ['claimId', 'sourceItemId', 'sourceRecordId']) string(binding[key], key);
}
function validatePayload(payload) {
  fields(payload, ['canonicalName', 'kind', 'claimId', 'statement', 'citation', 'searchCutoff', 'languageScope', 'limitations'], 'payload');
  requireThat(payload.kind === 'dated-review-study-design', 'Unsupported explanation kind');
  for (const key of ['canonicalName', 'claimId', 'statement', 'languageScope', 'limitations']) string(payload[key], key);
  requireThat(/^\d{4}-\d{2}-\d{2}$/.test(payload.searchCutoff) && new Date(payload.searchCutoff).toISOString().slice(0, 10) === payload.searchCutoff, 'Invalid search cutoff');
  const c = payload.citation;
  fields(c, ['displayLabel', 'sourceItemId', 'pmid', 'doi', 'url'], 'citation');
  for (const key of Object.keys(c)) string(c[key], `citation.${key}`);
  requireThat(/^\d+$/.test(c.pmid) && c.url === `https://pubmed.ncbi.nlm.nih.gov/${c.pmid}/`, 'Citation must identify the exact PubMed record');
  requireThat(/^10\.\d{4,9}\/\S+$/.test(c.doi), 'Invalid DOI');
}

export function project(bundle, now = new Date(), packaged = false) {
  fields(bundle, ['schemaVersion', 'entries', 'decisions'], 'bundle');
  requireThat(bundle.schemaVersion === '1.0.0' && Array.isArray(bundle.entries) && Array.isArray(bundle.decisions), 'Unsupported bundle');
  const nowMs = now.getTime();
  requireThat(Number.isFinite(nowMs), 'Invalid clock');
  const ids = new Set();
  for (const decision of bundle.decisions) {
    const decisionKeys = ['decisionId', 'compoundName', 'decision', 'reviewerId', 'reviewedAt', 'scope', 'notes', 'clearsSoftPromotionBlockers', 'expiresAt'];
    requireThat(Object.keys(decision).every(k => decisionKeys.includes(k)) && ['decisionId', 'compoundName', 'decision', 'reviewerId', 'reviewedAt', 'scope', 'notes'].every(k => Object.hasOwn(decision, k)), 'Invalid decision fields');
    string(decision.decisionId, 'decisionId');
    requireThat(!ids.has(decision.decisionId), 'Duplicate decisionId');
    ids.add(decision.decisionId);
    string(decision.compoundName, 'compoundName');
    string(decision.reviewerId, 'reviewerId');
    date(decision.reviewedAt, 'reviewedAt');
    if (Object.hasOwn(decision, 'clearsSoftPromotionBlockers')) requireThat(typeof decision.clearsSoftPromotionBlockers === 'boolean', 'Invalid promotion blocker flag');
    if (decision.expiresAt != null) date(decision.expiresAt, 'expiresAt');
    requireThat(['approve-claims', 'approve-for-promotion', 'resolve-review-items', 'request-changes', 'reject', 'archive-draft'].includes(decision.decision), 'Unknown decision kind');
    requireThat(decision.scope && Array.isArray(decision.scope.claimIds), 'Explicit claim scope required');
    const scopeKeys = ['claimIds', 'qualityFlags', 'reviewCategories', 'promotionBlockers', 'reviewQueueItemIds', 'remediationPlanItemIds', 'remediationResolutionTypes', 'displayBindings'];
    requireThat(Object.keys(decision.scope).every(k => scopeKeys.includes(k)), 'Unknown decision scope field');
    for (const key of ['claimIds', 'qualityFlags', 'reviewCategories', 'promotionBlockers']) requireThat(Array.isArray(decision.scope[key]) && decision.scope[key].every(v => typeof v === 'string'), `Invalid scope.${key}`);
    for (const key of scopeKeys.filter(k => k !== 'displayBindings' && Object.hasOwn(decision.scope, k))) requireThat(Array.isArray(decision.scope[key]) && decision.scope[key].every(v => typeof v === 'string') && new Set(decision.scope[key]).size === decision.scope[key].length, `Invalid/duplicate scope.${key}`);
    requireThat(Array.isArray(decision.notes) && decision.notes.every(v => typeof v === 'string'), 'Invalid decision notes');
    decision.scope.claimIds.forEach(id => string(id, 'scope.claimId'));
    if (decision.scope.displayBindings !== undefined) {
      requireThat(decision.decision === 'approve-claims' && decision.scope.claimIds.length === 1 && Array.isArray(decision.scope.displayBindings) && decision.scope.displayBindings.length === 1, 'Display approval requires exactly one approved claim/binding');
      validateBinding(decision.scope.displayBindings[0]);
      requireThat(decision.scope.displayBindings[0].claimId === decision.scope.claimIds[0], 'Binding claim differs from decision scope');
      requireThat(decision.clearsSoftPromotionBlockers !== true, 'Display approval must not clear compound promotion blockers');
    }
  }
  const names = new Set();
  const output = [];
  for (const entry of bundle.entries) {
    fields(entry, ['payload', 'claim', 'source', packaged ? 'evidenceHashes' : 'observationReceipt', 'decisionId'], 'entry');
    if (packaged) {
      fields(entry.claim, ['claimId', 'claimType', 'statement', 'sourceRefs'], 'packaged claim');
      fields(entry.source, ['sourceId', 'pmid', 'doi', 'url'], 'packaged source');
      fields(entry.evidenceHashes, ['claimSha256', 'sourceSnapshotSha256', 'observationReceiptSha256'], 'packaged evidence hashes');
    }
    validatePayload(entry.payload);
    const p = entry.payload;
    requireThat(!names.has(normalized(p.canonicalName)), 'Duplicate explanation compound');
    names.add(normalized(p.canonicalName));
    const d = bundle.decisions.find(d => d.decisionId === entry.decisionId);
    // A configured payload is never silently authorized by an unrelated approval.
    requireThat(d && d.decision === 'approve-claims' && d.scope.displayBindings?.length === 1, 'Missing explicit display approval');
    const b = d.scope.displayBindings[0];
    requireThat(normalized(d.compoundName) === normalized(p.canonicalName), 'Wrong approved compound');
    requireThat(b.claimId === p.claimId && entry.claim.claimId === p.claimId && entry.claim.statement === p.statement, 'Wrong claim/statement');
    requireThat(entry.claim.claimType === 'evidence-gap', 'Only evidence-context claims supported');
    requireThat(entry.claim.sourceRefs?.length === 1 && entry.claim.sourceRefs[0] === p.citation.sourceItemId, 'Exact single claim source required');
    requireThat(b.sourceItemId === p.citation.sourceItemId && b.sourceRecordId === p.citation.pmid, 'Wrong bound source identity');
    requireThat(entry.source.sourceId === p.citation.sourceItemId && entry.source.pmid === p.citation.pmid && entry.source.doi === p.citation.doi && entry.source.url === p.citation.url, 'Source/citation mismatch');
    for (const [key, value] of [['claimSha256', entry.claim], ['sourceSnapshotSha256', entry.source], ['observationReceiptSha256', entry.observationReceipt], ['payloadSha256', p]]) requireThat(b[key] === (packaged && key !== 'payloadSha256' ? entry.evidenceHashes[key] : hash(value)), `${key} mismatch`);
    requireThat(date(d.reviewedAt, 'reviewedAt') <= nowMs, 'Future approval');
    requireThat(d.expiresAt == null || Date.parse(d.expiresAt) > nowMs, 'Expired display approval');
    const blocker = bundle.decisions.some(other => normalized(other.compoundName) === normalized(p.canonicalName)
      && ['request-changes', 'reject', 'archive-draft'].includes(other.decision)
      && Date.parse(other.reviewedAt) >= Date.parse(d.reviewedAt) && Date.parse(other.reviewedAt) <= nowMs
      && (other.scope.claimIds.length === 0 || other.scope.claimIds.includes(p.claimId)));
    requireThat(!blocker, 'Later relevant decision blocks display');
    // The input may contain historical flags. Only this exact accepted payload is public.
    output.push({ ...p, acceptanceDecisionId: d.decisionId, expiresAt: d.expiresAt ?? null });
  }
  return output.sort((a, b) => a.canonicalName.localeCompare(b.canonicalName, 'en'));
}

// Root validation proves full canonical-object hashes before stripping private
// history. The frontend context can recheck bindings/expiry/revocation but cannot
// independently rehash omitted canonical records; CI checks that root linkage.
export function packageInput(bundle, now = new Date()) {
  project(bundle, now);
  return {
    schemaVersion: bundle.schemaVersion,
    entries: bundle.entries.map(e => ({
      payload: e.payload, decisionId: e.decisionId,
      claim: { claimId: e.claim.claimId, claimType: e.claim.claimType, statement: e.claim.statement, sourceRefs: e.claim.sourceRefs },
      source: { sourceId: e.source.sourceId, pmid: e.source.pmid, doi: e.source.doi, url: e.source.url },
      evidenceHashes: { claimSha256: hash(e.claim), sourceSnapshotSha256: hash(e.source), observationReceiptSha256: hash(e.observationReceipt) },
    })),
    decisions: bundle.decisions.map(d => ({
      decisionId: d.decisionId, compoundName: d.compoundName, decision: d.decision,
      reviewerId: d.reviewerId, reviewedAt: d.reviewedAt, expiresAt: d.expiresAt ?? null,
      clearsSoftPromotionBlockers: d.clearsSoftPromotionBlockers ?? false,
      scope: { claimIds: d.scope.claimIds, qualityFlags: [], reviewCategories: [], promotionBlockers: [],
        ...(d.scope.displayBindings ? { displayBindings: d.scope.displayBindings } : {}) }, notes: [],
    })),
  };
}

export async function checkOrWrite(frontendRoot, checkOnly = true) {
  const input = JSON.parse(await readFile(path.join(frontendRoot, 'reviewed-study-design/input.json'), 'utf8'));
  const output = `${JSON.stringify(project(input, new Date(), true), null, 2)}\n`;
  const target = path.join(frontendRoot, 'src/lib/generated/reviewed-study-design.json');
  if (checkOnly) requireThat(await readFile(target, 'utf8') === output, 'Stale reviewed study-design artifact; run root sync after review');
  else await writeFile(target, output, 'utf8');
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  await checkOrWrite(path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'), true);
  console.log('Reviewed study-design build input and public artifact match.');
}
