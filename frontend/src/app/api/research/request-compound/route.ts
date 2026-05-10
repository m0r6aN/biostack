import type { ResearchRequestBatch } from '@/lib/research/types';
import * as childProcess from 'node:child_process';
import * as fs from 'node:fs/promises';
import path from 'node:path';

export const runtime = 'nodejs';

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

function repoRoot() {
  if (process.env.RESEARCH_REPO_ROOT) return path.resolve(process.env.RESEARCH_REPO_ROOT);
  const cwd = process.cwd();
  return path.basename(cwd).toLowerCase() === 'frontend' ? path.resolve(cwd, '..') : cwd;
}

function safeName(value: string) {
  return value.replace(/[^a-zA-Z0-9._-]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 64) || 'compound';
}

function parseString(value: unknown) {
  return typeof value === 'string' ? value.trim() : '';
}

function parseStringArray(value: unknown) {
  if (Array.isArray(value)) return value.map(parseString).filter(Boolean);
  return parseString(value).split(',').map(item => item.trim()).filter(Boolean);
}

function toBatch(body: unknown): ResearchRequestBatch | null {
  if (!isRecord(body)) return null;
  const compoundName = parseString(body.compoundName);
  const rationale = parseString(body.rationale) || parseString(body.notes);
  if (!compoundName || !rationale) return null;

  const now = new Date().toISOString();
  const requesterId = parseString(body.requesterId) || 'research-ui';
  const priority = parseString(body.priority);
  const normalizedPriority = ['low', 'normal', 'high', 'urgent'].includes(priority) ? priority as ResearchRequestBatch['requests'][number]['priority'] : 'normal';
  const requestId = `research-request-${Date.now()}-${safeName(compoundName).toLowerCase()}`;
  return {
    schemaVersion: '1.0.0',
    recordType: 'research-request-batch',
    batch: { batchId: `batch-${requestId}`, requesterId, requestedAt: now, notes: [] },
    requests: [{
      requestId,
      compoundName,
      aliases: parseStringArray(body.aliases),
      classification: parseString(body.classification) || 'Other',
      priority: normalizedPriority,
      requesterId,
      requestedAt: now,
      rationale,
      notes: parseString(body.notes) ? [parseString(body.notes)] : [],
    }],
  };
}

async function saveBatch(root: string, batch: ResearchRequestBatch) {
  const directory = path.join(root, 'research', 'research-requests');
  await fs.mkdir(directory, { recursive: true });
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const filename = `research-request-${timestamp}-${safeName(batch.requests[0].compoundName)}.json`;
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
      ? setTimeout(() => { timedOut = true; child.kill(); }, timeoutMs)
      : null;

    child.stdout.setEncoding('utf8');
    child.stderr.setEncoding('utf8');
    child.stdout.on('data', chunk => { stdout = appendTail(stdout, chunk); });
    child.stderr.on('data', chunk => { stderr = appendTail(stderr, chunk); });
    child.on('error', error => { if (timer) clearTimeout(timer); reject(error); });
    child.on('close', exitCode => { if (timer) clearTimeout(timer); resolve({ exitCode, stdout, stderr, timedOut }); });
  });
}

export async function POST(request: Request) {
  if (process.env.NODE_ENV === 'production' || process.env.RESEARCH_AUTOMATION_ENABLED !== 'true') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  if (isProcessing) return Response.json({ error: 'Research worker is already processing a request.' }, { status: 409 });

  let body: unknown;
  try { body = await request.json(); } catch { return Response.json({ error: 'Invalid JSON body.' }, { status: 400 }); }
  const batch = toBatch(body);
  if (!batch) return Response.json({ error: 'Compound name and rationale are required.' }, { status: 400 });

  isProcessing = true;
  try {
    const root = repoRoot();
    const saved = await saveBatch(root, batch);
    const worker = await runResearchWorker(root);
    const payload = { savedFilename: saved.filename, savedPath: saved.target, researchRequestDirectory: saved.directory, ...worker };
    if (worker.timedOut) return Response.json({ ...payload, error: 'Research worker timed out.' }, { status: 504 });
    if (worker.exitCode !== 0) return Response.json({ ...payload, error: 'Research worker failed.' }, { status: 500 });
    return Response.json(payload);
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to request compound research.' }, { status: 500 });
  } finally {
    isProcessing = false;
  }
}