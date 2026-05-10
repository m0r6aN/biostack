import type { ReviewDecisionBatch, ReviewDecisionType } from '@/lib/research/types';
import * as childProcess from 'node:child_process';
import * as fs from 'node:fs/promises';
import path from 'node:path';

export const runtime = 'nodejs';

const DECISIONS: ReadonlySet<ReviewDecisionType> = new Set([
  'approve-for-promotion',
  'approve-claims',
  'resolve-review-items',
  'archive-draft',
  'request-changes',
  'reject',
]);

const MAX_LOG_CHARS = 12_000;
let isProcessing = false;

type WorkerResult = {
  exitCode: number | null;
  stdout: string;
  stderr: string;
  timedOut: boolean;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(item => typeof item === 'string');
}

function isIsoDate(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && !Number.isNaN(Date.parse(value));
}

function isReviewDecisionBatch(value: unknown): value is ReviewDecisionBatch {
  if (!isRecord(value)) return false;
  if (value.schemaVersion !== '1.0.0' || value.recordType !== 'review-decision-batch') return false;
  if (!isRecord(value.batch) || !Array.isArray(value.decisions) || value.decisions.length === 0) return false;
  if (typeof value.batch.batchId !== 'string' || value.batch.batchId.trim().length === 0) return false;
  if (typeof value.batch.reviewerId !== 'string' || value.batch.reviewerId.trim().length === 0) return false;
  if (!isIsoDate(value.batch.reviewedAt) || !isStringArray(value.batch.notes)) return false;

  return value.decisions.every(decision => {
    if (!isRecord(decision) || !isRecord(decision.scope)) return false;
    return typeof decision.decisionId === 'string'
      && decision.decisionId.trim().length > 0
      && typeof decision.compoundName === 'string'
      && decision.compoundName.trim().length > 0
      && typeof decision.decision === 'string'
      && DECISIONS.has(decision.decision as ReviewDecisionType)
      && typeof decision.reviewerId === 'string'
      && decision.reviewerId.trim().length > 0
      && isIsoDate(decision.reviewedAt)
      && isStringArray(decision.scope.claimIds)
      && isStringArray(decision.scope.reviewQueueItemIds)
      && isStringArray(decision.scope.qualityFlags)
      && isStringArray(decision.scope.reviewCategories)
      && isStringArray(decision.scope.promotionBlockers)
      && typeof decision.clearsSoftPromotionBlockers === 'boolean'
      && (decision.expiresAt === null || isIsoDate(decision.expiresAt))
      && isStringArray(decision.notes);
  });
}

function repoRoot() {
  if (process.env.RESEARCH_REPO_ROOT) return path.resolve(process.env.RESEARCH_REPO_ROOT);
  const cwd = process.cwd();
  return path.basename(cwd).toLowerCase() === 'frontend' ? path.resolve(cwd, '..') : cwd;
}

function safeName(value: string) {
  return value.replace(/[^a-zA-Z0-9._-]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 64) || 'batch';
}

async function saveBatch(root: string, batch: ReviewDecisionBatch) {
  const directory = path.join(root, 'research', 'review-decisions');
  await fs.mkdir(directory, { recursive: true });

  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const filename = `review-decision-batch-${timestamp}-${safeName(batch.batch.batchId)}.json`;
  const target = path.join(directory, filename);
  const temp = `${target}.${process.pid}.tmp`;

  try {
    await fs.writeFile(temp, `${JSON.stringify(batch, null, 2)}\n`, 'utf8');
    await fs.rename(temp, target);
  } catch (error) {
    await fs.unlink(temp).catch(() => undefined);
    throw error;
  }

  return { directory, filename, target };
}

function appendTail(current: string, chunk: unknown) {
  const next = `${current}${String(chunk)}`;
  return next.length > MAX_LOG_CHARS ? next.slice(next.length - MAX_LOG_CHARS) : next;
}

function runResearchWorker(root: string): Promise<WorkerResult> {
  const script = path.join(root, 'tools', 'research', 'run-knowledge-research.ps1');
  const shell = process.platform === 'win32' ? 'powershell.exe' : 'pwsh';
  const args = process.platform === 'win32'
    ? ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', script]
    : ['-NoProfile', '-File', script];
  const timeoutMs = Number.parseInt(process.env.RESEARCH_AUTOMATION_TIMEOUT_MS ?? '600000', 10);

  return new Promise((resolve, reject) => {
    let stdout = '';
    let stderr = '';
    let timedOut = false;
    const child = childProcess.spawn(shell, args, { cwd: root, env: process.env });
    const timer = Number.isFinite(timeoutMs) && timeoutMs > 0
      ? setTimeout(() => {
          timedOut = true;
          child.kill();
        }, timeoutMs)
      : null;

    child.stdout.setEncoding('utf8');
    child.stderr.setEncoding('utf8');
    child.stdout.on('data', chunk => { stdout = appendTail(stdout, chunk); });
    child.stderr.on('data', chunk => { stderr = appendTail(stderr, chunk); });
    child.on('error', error => {
      if (timer) clearTimeout(timer);
      reject(error);
    });
    child.on('close', exitCode => {
      if (timer) clearTimeout(timer);
      resolve({ exitCode, stdout, stderr, timedOut });
    });
  });
}

export async function POST(request: Request) {
  if (process.env.NODE_ENV === 'production' || process.env.RESEARCH_AUTOMATION_ENABLED !== 'true') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  if (isProcessing) {
    return Response.json({ error: 'Research worker is already processing a decision batch.' }, { status: 409 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: 'Invalid JSON body.' }, { status: 400 });
  }

  if (!isReviewDecisionBatch(body)) {
    return Response.json({ error: 'Invalid review decision batch.' }, { status: 400 });
  }

  isProcessing = true;
  try {
    const root = repoRoot();
    const saved = await saveBatch(root, body);
    const worker = await runResearchWorker(root);
    const payload = {
      savedFilename: saved.filename,
      savedPath: saved.target,
      reviewDecisionDirectory: saved.directory,
      ...worker,
    };

    if (worker.timedOut) {
      return Response.json({ ...payload, error: 'Research worker timed out.' }, { status: 504 });
    }

    if (worker.exitCode !== 0) {
      return Response.json({ ...payload, error: 'Research worker failed.' }, { status: 500 });
    }

    return Response.json(payload);
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to process review decision batch.' }, { status: 500 });
  } finally {
    isProcessing = false;
  }
}