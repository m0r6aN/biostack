import { readFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const grantPath = 'research/source-authorization/ligandrol-adverse-offline-grant-20260914.json';
export const scope = Object.freeze({ packetPath: 'research/input/evidence/ligandrol.evidence.json', claimId: 'ligandrol-adverse-effect-hormone-suppression-001', claimType: 'adverse-effect', sourceRegistryId: 'pubmed', sourceItemId: 'pubmed-22459616-basaria-research-20260913', pmid: '22459616', doi: '10.1093/gerona/gls078', sourceUrl: 'https://pubmed.ncbi.nlm.nih.gov/22459616/', field: 'contraindications-warnings', operation: 'offline-review-required-candidate-preparation' });
export const bindingPaths = Object.freeze(['research/source-authorization/owner-source-decisions-2026-09-08.v1.json', 'research/input/sources/pilot-source-registry.json', 'research/source-authorization/recommended-seven-source-decisions.basaria-alias-map-20260913.json', scope.packetPath]);
// Git checkouts may use CRLF; normalize only line endings, never JSON semantics.
const sha = b => createHash('sha256').update(b.toString('utf8').replace(/\r\n/g, '\n'), 'utf8').digest('hex');
const requireThat = (ok, why) => { if (!ok) throw new Error(why); };
const exact = (a,b) => JSON.stringify(a) === JSON.stringify(b);
function keys(x, allowed) { requireThat(x && typeof x === 'object' && !Array.isArray(x) && Object.keys(x).sort().join() === [...allowed].sort().join(), 'Unknown or missing fields'); }
function sameScope(x) { keys(x,Object.keys(scope)); for (const k of Object.keys(scope)) requireThat(x[k]===scope[k], `Scope mismatch: ${k}`); }

export async function authorize(root, request, suppliedGrant) {
  sameScope(request);
  const read = async p => readFile(path.join(root,p));
  const grant = suppliedGrant ?? JSON.parse(await read(grantPath));
  keys(grant,['schemaVersion','recordType','grantId','decision','decisionMaker','decisionReceipt','scope','bindings','restrictions','bindingEncoding']);
  requireThat(grant.bindingEncoding==='SHA256 of UTF8 file bytes with CRLF normalized to LF; all other bytes preserved','Unsupported binding encoding');
  requireThat(grant.schemaVersion==='1.0.0' && grant.recordType==='offline-item-field-grant' && grant.grantId==='ligandrol-adverse-offline-20260914' && grant.decision==='granted','Absent, pending or unsupported grant');
  keys(grant.decisionMaker,['personName','roleId']);requireThat(grant.decisionMaker.personName==='Clint Morgan' && grant.decisionMaker.roleId==='legal-rights-approver','Wrong decision maker');
  keys(grant.decisionReceipt,['ownerMessageVerbatim','originalDecisionAtUtc','recordedAtUtc','timestampBasis','conveyance','directSignatureClaimed','reviewedBriefPath','reviewedBriefSha256']);
  const d=grant.decisionReceipt;
  requireThat(d.ownerMessageVerbatim==="Ligandrol: grant the existing exact offline research scope in ligandrol-adverse-owner-decision-ready-20260914.md." && d.originalDecisionAtUtc===null && d.directSignatureClaimed===false && d.timestampBasis==='local-recording-time-not-owner-decision-time' && d.conveyance==='Owner response via status task, conveyed by initiative lead /root to support_readiness','Missing or altered conveyed decision receipt');
  requireThat(typeof d.recordedAtUtc==='string' && Number.isFinite(Date.parse(d.recordedAtUtc)) && Date.parse(d.recordedAtUtc)<=Date.now(),'Invalid receipt time');
  requireThat(d.reviewedBriefPath==='research/source-authorization/ligandrol-adverse-offline-grant-scope-20260914.md' && /^[a-f0-9]{64}$/.test(d.reviewedBriefSha256),'Invalid brief binding');
  requireThat(sha(await read(d.reviewedBriefPath))===d.reviewedBriefSha256,'Reviewed brief changed');
  sameScope(grant.scope);
  keys(grant.restrictions,['acquisitionAllowed','runtimeAllowed','sourcePassageTransmissionAllowed','rawTextRetentionAllowed','publicationAllowed','scientificApproval','candidateReviewStatus','factualScope']);
  for (const k of ['acquisitionAllowed','runtimeAllowed','sourcePassageTransmissionAllowed','rawTextRetentionAllowed','publicationAllowed','scientificApproval']) requireThat(grant.restrictions[k]===false,`Forbidden ${k}`);
  requireThat(grant.restrictions.candidateReviewStatus==='review-required' && grant.restrictions.factualScope==='Trial-attributed hormonal/lipid observations and bounded reported recovery only where retained facts support exact population/exposure/measurement context; no general safety or long-term reversibility conclusions, reassurance, clinical instructions or quantitative invention.','Restriction mismatch');
  requireThat(Array.isArray(grant.bindings) && grant.bindings.length===bindingPaths.length,'Missing current bindings');
  const objects=[];
  for(let i=0;i<bindingPaths.length;i++) { const b=grant.bindings[i];keys(b,['path','sha256']);requireThat(b.path===bindingPaths[i] && /^[a-f0-9]{64}$/.test(b.sha256),'Wrong binding path');const raw=await read(b.path);requireThat(sha(raw)===b.sha256,`Stale binding: ${b.path}`);objects.push(JSON.parse(raw)); }
  const [owners,registry,decisions,packet]=objects;
  requireThat(owners.decisions.some(d=>d.decisionId==='A1' && d.decision==='approved' && d.successorPersonName==='Clint Morgan' && d.roleId==='legal-rights-approver'),'Owner role not assigned');
  const classes=registry.sources.filter(s=>s.identity?.sourceId==='pubmed');requireThat(classes.length===1 && classes[0].identity.aliases.includes(scope.sourceItemId),'Source item unmapped');
  requireThat(classes[0].rights.reviewStatus==='approved' && classes[0].operations.status==='active' && classes[0].acquisition.enabled===true && classes[0].acquisition.method==='api','Registry source unavailable');
  const sources=decisions.sources.filter(s=>s.sourceId==='pubmed');requireThat(sources.length===1,'Source decision missing');const source=sources[0];
  requireThat(source.decisionStatus==='approved' && source.activationReady===true && source.acquisition.enabled===true && source.acquisition.method==='api' && source.rights.reviewStatus==='reviewed','Parent source unavailable');
  requireThat(exact(source.evidenceBoundary.authorizedFieldUse,['mechanism','efficacy-claims','interactions']),'Global PubMed fields changed');
  requireThat(packet.claims.filter(c=>c.claimId===scope.claimId && c.claimType===scope.claimType).length===1,'Claim identity mismatch');
  const items=packet.sources.filter(s=>s.sourceId===scope.sourceItemId);requireThat(items.length===1 && items[0].pmid===scope.pmid && items[0].doi===scope.doi && items[0].url===scope.sourceUrl,'Publication item mismatch');
  return { grantId:grant.grantId, allowedField:scope.field, operation:scope.operation, candidateReviewStatus:'review-required', acquisitionAllowed:false, publicationAllowed:false, factsSufficiency:'not-established-by-permission' };
}
if (process.argv[1] && path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  const requestPath=process.argv[2];requireThat(requestPath,'Supply exact offline request JSON path');
  const result=await authorize(path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),JSON.parse(await readFile(requestPath,'utf8')));
  console.log(JSON.stringify(result,null,2));
}
