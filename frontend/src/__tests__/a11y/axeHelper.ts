import axe from 'axe-core';
import type { AxeResults, Result } from 'axe-core';

/**
 * Impacts that fail the build. "moderate" and "minor" findings are tracked in
 * docs/INITIATIVES/accessibility/AUDIT.md instead of failing CI.
 */
const BLOCKING_IMPACTS = new Set(['serious', 'critical']);

/**
 * Rules that cannot give a trustworthy answer under jsdom (no layout, no computed colour) or that are
 * covered another way:
 * - color-contrast: needs real rendering. Contrast is enforced statically by contrastFloor.test.ts and
 *   measured in a real browser by scripts/a11y/scan.mjs (see AUDIT.md).
 * - region: reports the visually hidden skip link (a documented false positive: the skip link must sit
 *   before the landmarks). Landmark structure is asserted by the landmark tests instead.
 */
const JSDOM_UNRELIABLE_RULES = ['color-contrast', 'region'];

export async function runAxe(container: Element): Promise<AxeResults> {
  return axe.run(container, {
    runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'] },
    rules: Object.fromEntries(JSDOM_UNRELIABLE_RULES.map((id) => [id, { enabled: false }])),
    resultTypes: ['violations'],
  });
}

export function blockingViolations(results: AxeResults): Result[] {
  return results.violations.filter((violation) => violation.impact && BLOCKING_IMPACTS.has(violation.impact));
}

export function describeViolations(violations: Result[]): string {
  return violations
    .map((violation) => {
      const nodes = violation.nodes.map((node) => `    ${node.target.join(' ')}\n      ${node.html.slice(0, 140)}`).join('\n');
      return `${violation.impact} ${violation.id}: ${violation.help}\n${violation.helpUrl}\n${nodes}`;
    })
    .join('\n\n');
}

/** Fails with a readable list of every serious or critical WCAG 2.2 AA violation found in `container`. */
export async function expectNoBlockingViolations(container: Element, label: string): Promise<void> {
  const blocking = blockingViolations(await runAxe(container));
  if (blocking.length > 0) {
    throw new Error(`${label}: ${blocking.length} serious/critical accessibility violation(s)\n\n${describeViolations(blocking)}`);
  }
}
