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
  'src/components/protocols/ProviderObservationalSummary.tsx',
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

  it('the add form links a catalog record by slug only: no library dose, unit, frequency or time is ever read', () => {
    const source = read('src/components/compounds/CompoundForm.tsx');

    expect(source).not.toMatch(/recommendedDosage|standardDosageRange|maxReportedDose|weeklyDosageSchedule|tieredDosing|incrementalEscalationSteps|preferredTimeOfDay/);
    // KnowledgeEntry.frequency is the library's own text; the form must not read it
    expect(source).not.toMatch(/\b(entry|match|k|c)\.frequency\b/);
    // the only thing a pick copies into the form is the name (and the category when none was chosen)
    const linkTo = source.slice(source.indexOf('const linkTo'), source.indexOf('const typeaheadMatches'));
    expect(linkTo).not.toMatch(/setDose|setFrequency|dose|Dose/);
    // the client never sends a verification state or a category source
    expect(source).not.toMatch(/verificationState|categorySource/);
  });

  it('the taking list and provider summary never write a state or category: the server derives them', () => {
    for (const path of ['src/components/protocol-portal/tabs/TakingTab.tsx', 'src/components/protocols/ProviderObservationalSummary.tsx']) {
      const source = read(path);
      // response-only fields are read for labels, and dropped from the update body
      expect(source).not.toMatch(/verificationState:\s|categorySource:\s/);
    }
    expect(read('src/components/protocol-portal/tabs/TakingTab.tsx')).toMatch(/const \{ id, verificationState, categorySource, catalogSlug, \.\.\.rest \} = item;/);
  });
});
