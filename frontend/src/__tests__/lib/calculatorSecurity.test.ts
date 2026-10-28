import { buildCalculatorCsp, CALCULATOR_ROUTES, calculatorSecurityHeaders } from '@/lib/calculatorSecurity';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const root = resolve(__dirname, '../../..');
const read = (relative: string) => readFileSync(resolve(root, relative), 'utf8');

function directive(csp: string, name: string): string[] {
  const found = csp.split(';').map((part) => part.trim()).find((part) => part.startsWith(`${name} `));
  return found ? found.slice(name.length).trim().split(/\s+/) : [];
}

describe('calculator CSP', () => {
  it('limits connect-src to same-origin only (no hosts, no wildcards, no schemes)', () => {
    for (const env of ['production', 'development', 'test']) {
      expect(directive(buildCalculatorCsp(env), 'connect-src')).toEqual(["'self'"]);
    }
  });

  it('allows no third-party script host in any environment', () => {
    for (const env of ['production', 'development']) {
      const scripts = directive(buildCalculatorCsp(env), 'script-src');
      expect(scripts).toContain("'self'");
      expect(scripts.filter((item) => /^(https?:|\*|[a-z0-9.-]+\.[a-z]{2,})/i.test(item))).toEqual([]);
    }
    expect(directive(buildCalculatorCsp('production'), 'script-src')).not.toContain("'unsafe-eval'");
  });

  it('blocks frames and plugins and pins base-uri and form-action', () => {
    const csp = buildCalculatorCsp('production');
    expect(directive(csp, 'frame-src')).toEqual(["'none'"]);
    expect(directive(csp, 'object-src')).toEqual(["'none'"]);
    expect(directive(csp, 'base-uri')).toEqual(["'self'"]);
    expect(directive(csp, 'form-action')).toEqual(["'self'"]);
  });

  it('covers exactly the calculator routes and not the protocol analyzer', () => {
    const entries = calculatorSecurityHeaders('production');
    expect(entries.map((entry) => entry.source)).toEqual([...CALCULATOR_ROUTES]);
    expect(CALCULATOR_ROUTES).toEqual(['/tools', '/tools/reconstitution-calculator', '/tools/volume-calculator', '/tools/unit-converter']);
    expect(entries.map((entry) => entry.source)).not.toContain('/tools/analyzer');
    for (const entry of entries) {
      expect(entry.headers).toEqual([{ key: 'Content-Security-Policy', value: buildCalculatorCsp('production') }]);
    }
  });

  it('is wired into next.config.ts headers()', () => {
    const config = read('next.config.ts');
    expect(config).toContain('calculatorSecurityHeaders');
    expect(config).toMatch(/\.\.\.calculatorSecurityHeaders\(\)/);
  });

  it('only lists routes that exist as pages', () => {
    for (const route of CALCULATOR_ROUTES) {
      expect(() => read(`src/app${route}/page.tsx`)).not.toThrow();
    }
  });
});
