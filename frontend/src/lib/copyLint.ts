/**
 * Banned-phrase lint (voice guide §3). Runs on BioStack-authored copy: card text, alert
 * templates, ruling lines, myth pages and AI-assisted drafts. A hit blocks publish.
 * Claim statements quoted as people state them ("…") are exempt, since they're what we're
 * grading, and so are the reviewed refusal strings in REVIEWED_ALLOWLIST.
 * The Study details fold is the only exempt zone for dosing terms and is never linted.
 */
export interface LintHit { rule: string; match: string }

const W = (s: string) => new RegExp(`(?<![A-Za-z0-9])(?:${s})(?![A-Za-z0-9])`, 'i');

const RULES: Array<[string, RegExp]> = [
  // 3a dosing and administration
  ['3a:dose-number', /\b\d+(?:[.,]\d+)?\s?(?:mg|mcg|µg|ug|iu|units?|ml)\b/i],
  ['3a:dosing', W("you should take|take \\d+|start at|start with|titrate|run it at|dosing|dosage|beginner dose|safe dose|effective dose|how to use|how to take|best time to take")],
  ['3a:schedule', W('per day|per week|EOD|2x/week|on cycle|off cycle|PCT|blast and cruise')],
  ['3a:stacking', W('protocol for|stack with|stack it with|pairs well with|combine with')],
  ['3a:administration', W('reconstitute|inject|subq|sub-q|draw \\d+ units')],
  // 3b endorsement
  ['3b:endorsement', W("we recommend|recommended|our pick|best (?:peptide|sarm|compound)s?|top \\d+ peptides for|better than|stronger than|more effective than|clinically proven|side-effect free|no side effects|natural so it's safe|worth trying|give it a shot|can't hurt|game changer|miracle|cures?")],
  ['3b:safe', W('safe|safe for')],
  // 3c commerce
  ['3c:commerce', W('buy|where to buy|get it from|source it|sourcing|vendor|supplier|research chem|discount code|coupon|link in bio|affiliate|sponsored')],
  // 3d people-targeting
  ['3d:people', /(^|\s)@[A-Za-z0-9_]{2,}|\bthis (?:influencer|guy)\b|\b(?:idiot|clown|grifter|scammer|shill)\b/i],
];

/** Reviewed system strings (§4) that refuse dosing; they may contain otherwise-banned words. */
export const NO_DOSING_RESPONSE =
  "We don't answer dosing questions. Not to dodge, but because the right answer depends on you, and we don't know you. What we can show you: the conditions used in trials, if any exist.";
export const REVIEWED_ALLOWLIST = new Set<string>([NO_DOSING_RESPONSE]);

const QUOTED = /“[^”]*”|"[^"]*"/g;

export function lintCopy(text: string, opts: { allowProven?: boolean } = {}): LintHit[] {
  if (!text || REVIEWED_ALLOWLIST.has(text)) return [];
  const unquoted = text.replace(QUOTED, ' ');
  const hits: LintHit[] = [];
  for (const [rule, re] of RULES) {
    const m = unquoted.match(re);
    if (m) hits.push({ rule, match: m[0].trim() });
  }
  // "proven" is allowed only as the PROVEN IN HUMANS chip with its qualifier.
  const proven = unquoted.replace(/PROVEN IN HUMANS/g, ' ').match(/\bproven\b/i);
  if (proven && !opts.allowProven) hits.push({ rule: '3b:proven', match: proven[0] });
  return hits;
}

export function assertCleanCopy(parts: string[], opts: { allowProven?: boolean } = {}): void {
  const hits = parts.flatMap(p => lintCopy(p, opts));
  if (hits.length > 0) throw new CopyLintError(hits);
}

export class CopyLintError extends Error {
  constructor(public readonly hits: LintHit[]) {
    super(`Banned phrase(s): ${hits.map(h => `${h.rule} "${h.match}"`).join(', ')}`);
    this.name = 'CopyLintError';
  }
}
