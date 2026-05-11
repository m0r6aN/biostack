import { POST } from '@/app/api/research/request-compound/route';
import { spawn } from 'node:child_process';
import { EventEmitter } from 'node:events';
import { mkdir, readFile, rename, unlink, writeFile } from 'node:fs/promises';
import path from 'node:path';

vi.mock('node:fs/promises', () => ({
  default: {},
  mkdir: vi.fn().mockResolvedValue(undefined),
  readFile: vi.fn().mockResolvedValue(JSON.stringify({
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T00:00:00Z',
    categories: [
      { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
      { name: 'Longevity', aliases: ['longevity'] },
      { name: 'Mitochondrial Support', aliases: ['mitochondrial support'] },
    ],
  })),
  writeFile: vi.fn().mockResolvedValue(undefined),
  rename: vi.fn().mockResolvedValue(undefined),
  unlink: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('node:child_process', () => ({ default: {}, spawn: vi.fn() }));

function request(body: unknown) {
  return new Request('http://localhost/api/research/request-compound', { method: 'POST', body: JSON.stringify(body) });
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

describe('research request-compound route', () => {
  const originalEnabled = process.env.RESEARCH_AUTOMATION_ENABLED;

  beforeEach(() => {
    process.env.RESEARCH_AUTOMATION_ENABLED = 'true';
    vi.mocked(mkdir).mockClear();
    vi.mocked(readFile).mockClear();
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

    const response = await POST(request({ compoundName: 'Epitalon', rationale: 'New peptide request.' }));

    expect(response.status).toBe(404);
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });

  it('saves a research request and runs the worker', async () => {
    const response = await POST(request({
      compoundName: 'Epitalon',
      aliases: 'Epithalon',
      categories: 'nootropic, Nootropics, mitochondrial support',
      rationale: 'Add to BioStack research backlog.',
    }));
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.savedFilename).toMatch(/^research-request-/);
    expect(payload.exitCode).toBe(0);
    expect(vi.mocked(mkdir)).toHaveBeenCalledWith(expect.stringContaining(`${path.sep}research${path.sep}research-requests`), { recursive: true });
    expect(vi.mocked(readFile)).toHaveBeenCalledWith(expect.stringContaining(`${path.sep}research${path.sep}category-taxonomy.json`), 'utf8');
    expect(vi.mocked(writeFile)).toHaveBeenCalledWith(expect.stringContaining('.tmp'), expect.stringContaining('"recordType": "research-request-batch"'), 'utf8');
    expect(vi.mocked(writeFile).mock.calls[0]?.[1]).toContain('"categories": [');
    expect(vi.mocked(writeFile).mock.calls[0]?.[1]).toContain('"Nootropics"');
    expect(vi.mocked(writeFile).mock.calls[0]?.[1]).toContain('"Mitochondrial Support"');
    expect(vi.mocked(rename)).toHaveBeenCalled();
    expect(vi.mocked(spawn)).toHaveBeenCalledWith(expect.any(String), expect.arrayContaining(['-File', expect.stringContaining('run-knowledge-research.ps1')]), expect.objectContaining({ cwd: expect.any(String) }));
  });

  it('requires a compound name and rationale', async () => {
    const response = await POST(request({ compoundName: 'Epitalon' }));

    expect(response.status).toBe(400);
    expect(await response.json()).toEqual({ error: 'Compound name and rationale are required.' });
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });
});