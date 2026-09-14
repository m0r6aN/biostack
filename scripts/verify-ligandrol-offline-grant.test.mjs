import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir, mkdtemp, rm } from 'node:fs/promises';
import path from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { authorize,scope,grantPath,bindingPaths } from './verify-ligandrol-offline-grant.mjs';
const root=fileURLToPath(new URL('../',import.meta.url));
const load=async()=>JSON.parse(await readFile(new URL('../'+grantPath,import.meta.url),'utf8'));
test('exact actual owner grant permits only offline review-required preparation',async()=>{
 const r=await authorize(root,{...scope});assert.equal(r.allowedField,'contraindications-warnings');assert.equal(r.acquisitionAllowed,false);assert.equal(r.publicationAllowed,false);assert.equal(r.factsSufficiency,'not-established-by-permission');
});
for(const [name,mutate] of [
 ['pending',g=>g.decision='pending'],['wrong role',g=>g.decisionMaker.roleId='evidence-reviewer'],['fabricated original timestamp',g=>g.decisionReceipt.originalDecisionAtUtc='2026-09-14T00:00:00Z'],['missing receipt',g=>delete g.decisionReceipt],['unknown grant property',g=>g.approved=true],['unsupported version',g=>g.schemaVersion='2.0.0'],['stale registry',g=>g.bindings[1].sha256='0'.repeat(64)],['stale owner assignment',g=>g.bindings[0].sha256='0'.repeat(64)],['changed packet',g=>g.bindings[3].sha256='0'.repeat(64)],['missing binding',g=>g.bindings.pop()],['runtime expansion',g=>g.restrictions.runtimeAllowed=true],['egress expansion',g=>g.restrictions.sourcePassageTransmissionAllowed=true],['grant wildcard',g=>g.scope.claimId='*'],['unknown restriction',g=>g.restrictions.training=true],['brief mismatch',g=>g.decisionReceipt.reviewedBriefSha256='0'.repeat(64)]
]) test(`rejects ${name}`,async()=>{const g=await load();mutate(g);await assert.rejects(authorize(root,{...scope},g));});
for(const [key,value] of [['claimId','other-claim'],['sourceItemId','other-item'],['pmid','999'],['doi','10.0000/other'],['sourceUrl','https://example.org'],['field','interactions'],['operation','api-acquisition'],['claimType','efficacy'],['packetPath','../other.json']])test(`rejects request ${key} mismatch`,async()=>{await assert.rejects(authorize(root,{...scope,[key]:value}));});
test('rejects request extra fields',async()=>{await assert.rejects(authorize(root,{...scope,runtime:true}));});
for (const ending of ['LF','CRLF']) test(`actual grant verifies against ${ending} checkout bytes`,async()=>{
 const dir=await mkdtemp(path.join(tmpdir(),'ligandrol-grant-lines-'));
 try { const g=await load();for(const file of [grantPath,g.decisionReceipt.reviewedBriefPath,...bindingPaths]) {
  const target=path.join(dir,file);await mkdir(path.dirname(target),{recursive:true});
  const text=(await readFile(path.join(root,file),'utf8')).replace(/\r\n/g,'\n');await writeFile(target,ending==='CRLF'?text.replace(/\n/g,'\r\n'):text,'utf8');
 } assert.equal((await authorize(dir,{...scope})).candidateReviewStatus,'review-required');
 } finally { await rm(dir,{recursive:true,force:true}); }
});
