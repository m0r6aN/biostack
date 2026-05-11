import { POST } from '@/app/api/research/process-decisions/route';
import type { ReviewDecisionBatch } from '@/lib/research/types';
import { spawn } from 'node:child_process';
import { EventEmitter } from 'node:events';
import { mkdir, rename, unlink, writeFile } from 'node:fs/promises';
import path from 'node:path';

vi.mock('node:fs/promises', () => ({
  default: {},
  mkdir: vi.fn().mockResolvedValue(undefined),
  writeFile: vi.fn().mockResolvedValue(undefined),
  rename: vi.fn().mockResolvedValue(undefined),
  unlink: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('node:child_process', () => ({ default: {}, spawn: vi.fn() }));

const validBatch: ReviewDecisionBatch = {
  schemaVersion: '1.0.0',
  recordType: 'review-decision-batch',
  batch: { batchId: 'batch-test', reviewerId: 'r1', reviewedAt: '2026-05-10T12:00:00.000Z', notes: [] },
  decisions: [{
    decisionId: 'dec-1', compoundName: 'Semaglutide', decision: 'resolve-review-items', reviewerId: 'r1',
    reviewedAt: '2026-05-10T12:01:00.000Z', clearsSoftPromotionBlockers: false, expiresAt: null, notes: [],
    scope: { claimIds: [], reviewQueueItemIds: ['queue-1'], qualityFlags: [], reviewCategories: [], promotionBlockers: [] },
  }],
};

function request(body: unknown) {
  return new Request('http://localhost/api/research/process-decisions', { method: 'POST', body: JSON.stringify(body) });
}

function successfulChild() {
  const child = new EventEmitter() as EventEmitter & {
    stdout: EventEmitter & { setEncoding: ReturnType<typeof vi.fn> };
    stderr: EventEmitter & { setEncoding: ReturnType<typeof vi.fn> };
    kill: ReturnType<typeof vi.fn>;
  };
  child.stdout = Object.assign(new EventEmitter(), { setEncoding: vi.fn() });
  child.stderr = Object.assign(new EventEmitter(), { setEncoding: vi.fn() });
  child.kill = vi.fn();
  setTimeout(() => {
    child.stdout.emit('data', '[BioStack Research] Complete.');
    child.emit('close', 0);
  }, 0);
  return child;
}

describe('research process-decisions route', () => {
  const originalEnabled = process.env.RESEARCH_AUTOMATION_ENABLED;

  beforeEach(() => {
    process.env.RESEARCH_AUTOMATION_ENABLED = 'true';
    vi.mocked(mkdir).mockClear();
    vi.mocked(writeFile).mockClear();
    vi.mocked(rename).mockClear();
    vi.mocked(unlink).mockClear();
    vi.mocked(spawn).mockReset().mockImplementation(() => successfulChild() as never);
  });

  afterEach(() => {
    if (originalEnabled === undefined) delete process.env.RESEARCH_AUTOMATION_ENABLED;
    else process.env.RESEARCH_AUTOMATION_ENABLED = originalEnabled;
  });

  it('404s unless local research automation is explicitly enabled', async () => {
    delete process.env.RESEARCH_AUTOMATION_ENABLED;

    const response = await POST(request(validBatch));

    expect(response.status).toBe(404);
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });

  it('validates, saves, and runs the research worker', async () => {
    const response = await POST(request(validBatch));
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.savedFilename).toMatch(/^review-decision-batch-/);
    expect(payload.exitCode).toBe(0);
    expect(vi.mocked(mkdir)).toHaveBeenCalledWith(expect.stringContaining(`${path.sep}research${path.sep}review-decisions`), { recursive: true });
    expect(vi.mocked(writeFile)).toHaveBeenCalledWith(expect.stringContaining('.tmp'), expect.stringContaining('"recordType": "review-decision-batch"'), 'utf8');
    expect(vi.mocked(rename)).toHaveBeenCalled();
    expect(vi.mocked(spawn)).toHaveBeenCalledWith(expect.any(String), expect.arrayContaining(['-File', expect.stringContaining('run-knowledge-research.ps1')]), expect.objectContaining({ cwd: expect.any(String) }));
  });

  it('rejects empty decision batches before writing files', async () => {
    const response = await POST(request({ ...validBatch, decisions: [] }));

    expect(response.status).toBe(400);
    expect(await response.json()).toEqual({ error: 'Invalid review decision batch.' });
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });
});