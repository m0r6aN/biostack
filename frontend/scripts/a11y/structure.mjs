// Automated stand-in for a first screen-reader pass (not part of CI, and not a substitute for NVDA or VoiceOver).
// For each route at desktop and mobile widths it records: landmarks and their names, heading outline and level
// skips, controls with no accessible name, images without alt, unlabeled fields, live regions, the skip link,
// the document title and language, and the first 30 Tab stops (name, region, visible focus indicator).
// Usage: node scripts/a11y/structure.mjs https://biostack.cc /tmp/structure.json /,/knowledge,/support
// Read-only: it only issues GET navigations and key presses; it never submits a form.
import { chromium } from 'playwright';
import { writeFileSync } from 'node:fs';
const base = process.argv[2]; const out = process.argv[3];
const routes = process.argv[4].split(',');
const b = await chromium.launch();
const report = {};
for (const vp of [{n:'desktop',w:1280,h:900},{n:'mobile',w:390,h:844}]) {
  const ctx = await b.newContext({ viewport:{width:vp.w,height:vp.h} });
  for (const r of routes) {
    const p = await ctx.newPage();
    await p.goto(base + r, { waitUntil:'networkidle' }); await p.waitForTimeout(800);
    const info = await p.evaluate(() => {
      const vis = el => { const s = getComputedStyle(el); const rc = el.getBoundingClientRect(); return s.visibility!=='hidden' && s.display!=='none' && (rc.width>0||rc.height>0) && !el.closest('[inert],[aria-hidden="true"]'); };
      const name = el => (el.getAttribute('aria-label') || (el.getAttribute('aria-labelledby')||'').split(' ').map(i=>document.getElementById(i)?.textContent||'').join(' ') || el.labels?.[0]?.textContent || el.textContent || el.getAttribute('alt') || el.getAttribute('title') || el.getAttribute('placeholder') || '').replace(/\s+/g,' ').trim();
      const lm = [...document.querySelectorAll('header,nav,main,aside,footer,section[aria-label],section[aria-labelledby],form[aria-label],[role=banner],[role=navigation],[role=main],[role=complementary],[role=contentinfo],[role=search],[role=region]')].filter(vis).map(e => ({ tag: e.tagName.toLowerCase(), role: e.getAttribute('role'), name: e.getAttribute('aria-label') || (e.getAttribute('aria-labelledby') ? name(e) : '') }));
      const heads = [...document.querySelectorAll('h1,h2,h3,h4,h5,h6,[role=heading]')].filter(vis).map(h => ({ l: +(h.tagName[1] || h.getAttribute('aria-level')), t: name(h).slice(0,60) }));
      const skips = []; let prev = 0; for (const h of heads) { if (prev && h.l > prev + 1) skips.push(`h${prev}->h${h.l} "${h.t}"`); prev = h.l; }
      const inter = [...document.querySelectorAll('a[href],button,input,select,textarea,[role=button],[role=link],[role=tab],[role=checkbox],[role=switch],[tabindex]')].filter(vis);
      const unnamed = inter.filter(e => e.tagName!=='INPUT' || e.type!=='hidden').filter(e => !name(e) ).map(e => e.outerHTML.slice(0,140));
      const genericNames = inter.filter(e => /^(click here|here|read more|more|link|button|learn more)$/i.test(name(e))).map(e=>name(e)+' -> '+(e.getAttribute('href')||''));
      const dupNames = {}; inter.filter(e=>e.tagName==='A').forEach(e => { const k = name(e).toLowerCase()+'|'+e.getAttribute('href'); });
      const imgs = [...document.querySelectorAll('img')].filter(vis).filter(i => !i.hasAttribute('alt')).map(i => i.outerHTML.slice(0,120));
      const live = [...document.querySelectorAll('[role=status],[role=alert],[aria-live],[role=log]')].map(e => ({ role: e.getAttribute('role'), live: e.getAttribute('aria-live'), text: (e.textContent||'').trim().slice(0,50) }));
      const forms = [...document.querySelectorAll('input:not([type=hidden]),select,textarea')].filter(vis).map(e => ({ type: e.type, name: name(e), required: e.required, autocomplete: e.autocomplete })).filter(f=>!f.name);
      const skipLink = [...document.querySelectorAll('a[href^="#"]')].find(a => /skip/i.test(a.textContent||''));
      return { title: document.title, lang: document.documentElement.lang, h1Count: heads.filter(h=>h.l===1).length, landmarks: lm, headings: heads, headingSkips: skips, unnamed, genericNames, imgsNoAlt: imgs, live, unlabeledFields: forms, skipLink: skipLink ? { text: skipLink.textContent.trim(), target: skipLink.getAttribute('href'), targetExists: !!document.querySelector(skipLink.getAttribute('href')) } : null, mainCount: document.querySelectorAll('main,[role=main]').length, bannerCount: document.querySelectorAll('header').length };
    });
    // keyboard order: first 30 tab stops
    const tabs = [];
    await p.evaluate(() => { document.activeElement && document.activeElement.blur(); });
    let lastTop = -1, backward = 0;
    for (let i = 0; i < 30; i++) {
      await p.keyboard.press('Tab');
      const t = await p.evaluate(() => { const e = document.activeElement; if (!e || e === document.body) return null; const rc = e.getBoundingClientRect(); const cs = getComputedStyle(e); const nm = (e.getAttribute('aria-label') || e.textContent || e.getAttribute('placeholder') || '').replace(/\s+/g,' ').trim().slice(0,40); return { tag: e.tagName.toLowerCase(), nm, top: Math.round(rc.top + window.scrollY), left: Math.round(rc.left), inView: rc.bottom>0 && rc.top<innerHeight, outline: cs.outlineStyle !== 'none' && parseFloat(cs.outlineWidth) > 0, region: e.closest('header,nav,main,aside,footer')?.tagName.toLowerCase() || '-' }; });
      if (!t) { tabs.push(null); continue; }
      if (lastTop >= 0 && t.top < lastTop - 40) backward++;
      lastTop = t.top; tabs.push(t);
    }
    info.tabStops = tabs.map(t => t ? `${t.region}:${t.tag}:${t.nm}${t.outline?'':' [NO-OUTLINE]'}` : 'body');
    info.tabNoOutline = tabs.filter(t => t && !t.outline).map(t => t.nm);
    info.tabBackwardJumps = backward;
    report[`${vp.n} ${r}`] = info;
    await p.close();
  }
  await ctx.close();
}
await b.close();
writeFileSync(out, JSON.stringify(report, null, 1));
for (const [k, v] of Object.entries(report)) console.log(k, '| title:', v.title, '| lang', v.lang, '| h1', v.h1Count, '| main', v.mainCount, '| skipLink', JSON.stringify(v.skipLink), '| hSkips', v.headingSkips.length, '| unnamed', v.unnamed.length, '| noAlt', v.imgsNoAlt.length, '| unlabeled', v.unlabeledFields.length, '| noOutline', v.tabNoOutline.length, '| backJumps', v.tabBackwardJumps, '| live', v.live.length);
