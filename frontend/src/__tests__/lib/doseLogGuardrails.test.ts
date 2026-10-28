import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const stripComments = (source: string) => source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');
const read = (path: string) => stripComments(readFileSync(resolve(process.cwd(), path), 'utf8'));

const SOURCES = [
  'src/lib/userFrequency.ts',
  'src/components/compounds/FrequencyFields.tsx',
  'src/components/protocol-portal/DoseLogForm.tsx',
  'src/components/protocol-portal/DoseHistory.tsx',
  'src/components/protocol-portal/ScheduleItemDetails.tsx',
  'src/components/protocol-portal/tabs/TakingTab.tsx',
];

describe('BioStack never suggests, prefills or computes a dose or schedule', () => {
  it.each(SOURCES)('%s does not import the calculator, the knowledge library or any dosing helper', (path) => {
    const source = read(path);

    expect(source).not.toMatch(/from\s+['"][^'"]*(calculator|knowledge|dosing|recommend|library)[^'"]*['"]/i);
    expect(source).not.toMatch(/recommendedDosage|standardDosageRange|maxReportedDose|desiredDoseMcg/);
  });

  it('the log form starts every input blank and only copies the saved entry from an explicit button press', () => {
    const source = read('src/components/protocol-portal/DoseLogForm.tsx');

    expect(source).toContain("useState('')");
    expect(source.match(/useState\(''\)/g)).toHaveLength(3);
    // The saved entry is read only inside the "same as my saved entry" click handler.
    const initialisers = source.match(/useState[^;]*;/g) ?? [];
    for (const initialiser of initialisers) {
      expect(initialiser).not.toMatch(/doseAmount|doseUnit|savedEntry/);
    }
    expect(source).toMatch(/onClick=\{\(\) => \{\s*setAmount\(String\(savedEntry\.amount\)\)/);
  });

  it('the frequency form starts blank and never derives a kind from the free-text frequency', () => {
    const lib = read('src/lib/userFrequency.ts');
    const fields = read('src/components/compounds/FrequencyFields.tsx');

    expect(lib).toMatch(/kind: '',\s*intervalDays: '',\s*weekdays: \[\],/);
    expect(lib).not.toMatch(/doseFrequency\.(match|includes|toLowerCase|test)/);
    // The only place a kind is ever set is the select the user changes.
    expect(fields.match(/set\(\{ kind:/g)).toHaveLength(1);
    expect(fields).toMatch(/onChange=\{\(e\) => set\(\{ kind: e\.target\.value \}\)\}/);
  });
});
