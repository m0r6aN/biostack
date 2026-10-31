// Playwright + axe-core scan used for docs/INITIATIVES/accessibility/AUDIT.md.
// Not part of CI (CI runs the vitest axe gate in src/__tests__/a11y). Needs `playwright` installed separately
// (`npm i --no-save playwright && npx playwright install chromium`).
// Usage: node scripts/a11y/scan.mjs <baseUrl> </route,/route,...> <out.json> [storageState.json]
// Runs the WCAG 2.0/2.1/2.2 A+AA and best-practice axe rule sets at 1280px and 390px.
import { chromium } from 'playwright';
import { readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const axeSrc = readFileSync(require.resolve('axe-core/axe.min.js'), 'utf8');
const base = process.argv[2];
const routes = process.argv[3].split(',');
const out = process.argv[4];
const storage = process.argv[5]; // optional storageState
const browser = await chromium.launch();
const results = {};
for (const vp of [{name:'desktop',width:1280,height:900},{name:'mobile',width:390,height:844}]) {
  const ctx = await browser.newContext({ viewport: vp, ...(storage ? { storageState: storage } : {}) });
  for (const r of routes) {
    const page = await ctx.newPage();
    try {
      await page.goto(base + r, { waitUntil: 'networkidle', timeout: 45000 });
      await page.waitForTimeout(800);
      await page.evaluate(axeSrc);
      const res = await page.evaluate(async () => await axe.run(document, { runOnly: { type: 'tag', values: ['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa','best-practice'] } }));
      results[`${vp.name} ${r}`] = { url: page.url(), violations: res.violations.map(v => ({ id: v.id, impact: v.impact, help: v.help, tags: v.tags.filter(t=>t.startsWith('wcag')&&t.length>6), nodes: v.nodes.length, sample: v.nodes.slice(0,3).map(n => ({ target: n.target.join(' '), html: n.html.slice(0,160), msg: (n.any[0]||n.all[0]||n.none[0]||{}).message })) })) };
    } catch (e) { results[`${vp.name} ${r}`] = { error: String(e).slice(0,200) }; }
    await page.close();
  }
  await ctx.close();
}
await browser.close();
writeFileSync(out, JSON.stringify(results, null, 1));
for (const [k, v] of Object.entries(results)) {
  if (v.error) { console.log(k, 'ERROR', v.error); continue; }
  const s = v.violations.map(x => `${x.id}(${x.impact},${x.nodes})`).join(' ');
  console.log(k, v.url !== base + k.split(' ')[1] ? `[-> ${v.url}]` : '', s || 'clean');
}
