import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const template = readFileSync(
  resolve(process.cwd(), 'templates/protocol_template.html'),
  'utf8',
);

describe('protocol template qualification', () => {
  it('does not report persisted dose activity from the static reference preview', () => {
    expect(template).not.toContain('onclick="logDose()"');
    expect(template).not.toContain('function logDose()');
    expect(template).not.toContain('Doses successfully logged for today.');
    expect(template).not.toContain('Doses Logged');
  });

  it('marks the dose control as a disabled, non-persisting preview', () => {
    const doseButton = template.match(
      /<button[^>]*title="This reference preview does not save dose activity\."[\s\S]*?<\/button>/,
    )?.[0];

    expect(doseButton).toBeDefined();
    expect(doseButton).toContain('disabled aria-disabled="true"');
    expect(doseButton).toContain(
      'Reference preview — dose activity is not saved',
    );
  });
});
