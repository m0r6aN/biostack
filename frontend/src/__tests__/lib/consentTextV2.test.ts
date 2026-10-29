import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { APPROVED_PATH, approvedSections, plain } from './consentApproved';
import {
  CONSENT_BUTTON_AGREE,
  CONSENT_BUTTON_DECLINE,
  CONSENT_DETAILS,
  CONSENT_SHORT_VERSION,
  CONSENT_TITLE_NEW,
  CONSENT_TITLE_RETURNING,
  CONSENT_VERSION_V2,
  CONSENT_WHAT_CHANGED,
  bulletText,
} from '@/lib/consentTextV2';

describe('consent v2 text', () => {
  const sections = approvedSections();
  const key = (prefix: string) => Object.keys(sections).find((name) => name.startsWith(prefix))!;

  it('pins the version', () => {
    expect(CONSENT_VERSION_V2).toBe('bio-observational-v2');
  });

  it('has both titles exactly as approved', () => {
    const [titleLine] = sections[key('Screen title')];
    const bold = [...titleLine.matchAll(/\*\*([^*]+)\*\*/g)].map((m) => m[1]);
    expect(bold).toEqual([CONSENT_TITLE_NEW, CONSENT_TITLE_RETURNING]);
  });

  it('has the short version exactly as approved', () => {
    const approved = sections[key('Short version')].filter((l) => l.startsWith('>')).map((l) => plain(l.replace(/^>\s*/, '')));
    expect(approved).toHaveLength(4);
    expect(CONSENT_SHORT_VERSION.map(plain)).toEqual(approved);
  });

  it('has the buttons exactly as approved', () => {
    const buttons = readFileSync(APPROVED_PATH, 'utf8').split('\n').find((l) => l.startsWith('Buttons:'))!;
    expect([...buttons.matchAll(/\*\*([^*]+)\*\*/g)].map((m) => m[1])).toEqual([CONSENT_BUTTON_AGREE, CONSENT_BUTTON_DECLINE]);
  });

  it('has every details section, bullet and paragraph exactly as approved', () => {
    const lines = sections[key('Details')];
    const approved: string[] = [];
    for (const line of lines) {
      approved.push(plain(line.replace(/^-\s+/, '')));
    }
    const ours: string[] = [];
    for (const section of CONSENT_DETAILS) {
      ours.push(plain(section.heading));
      if (section.paragraph) ours.push(plain(section.paragraph));
      for (const bullet of section.bullets ?? []) ours.push(plain(bulletText(bullet)));
    }
    expect(ours).toEqual(approved);
  });

  it('has the "what changed" box exactly as approved', () => {
    const [line] = sections[key('"What changed"')];
    expect(plain(CONSENT_WHAT_CHANGED)).toBe(plain(line.replace(/^>\s*/, '')));
  });

  it('describes every future item Clint required, with the opt-in promise', () => {
    const text = CONSENT_DETAILS.flatMap((s) => [s.paragraph ?? '', ...(s.bullets ?? []).map(bulletText)]).join(' ') + CONSENT_SHORT_VERSION.join(' ');
    for (const needle of [
      'dose logs', "Everything I'm Taking", 'custom items', 'Provider summary', 'NIH/NLM DSLD', 'PubMed', 'ClinicalTrials.gov', 'openFDA',
      'PubChem', 'RxNorm', 'Open Food Facts', 'keep private', 'Anonymous research requests', 'web push', 'generic', 'Export:', 'Delete:',
      '35 days', "doesn't prescribe, suggest, or work out a dose", "We don't sell your data", 'we ask again with an on/off switch',
    ]) {
      expect(text, needle).toContain(needle);
    }
    expect(text.toLowerCase()).not.toContain('affiliate');
  });
});
