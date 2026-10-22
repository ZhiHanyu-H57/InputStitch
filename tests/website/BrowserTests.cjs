'use strict';
// Isolated headless developer tests; never use a user's browser profile.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const base = process.env.SITE_BASE_URL || 'http://127.0.0.1:7857';
const out = process.env.SITE_TEST_OUTPUT || path.join(__dirname, '../../artifacts/website');
fs.mkdirSync(out, { recursive: true });
const snapshot = JSON.parse(fs.readFileSync(path.join(__dirname, '../../website/assets/release.json'), 'utf8'));
const stable = { tag_name: `v${snapshot.version}`, draft: false, prerelease: false, published_at: snapshot.publishedAt,
  assets: Object.entries(snapshot.assets).map(([arch, data]) => ({ name: `InputStitch-${snapshot.version}-Windows-${arch}.exe`, size: data.size })) };
let checks = 0;
function check(value, label) { assert.ok(value, label); checks++; }
async function fixture(context, data = stable) {
  await context.route('https://api.github.com/repos/ZhiHanyu-H57/InputStitch/releases/latest', route => route.fulfill({
    contentType: 'application/json', headers: { 'access-control-allow-origin': '*' }, body: JSON.stringify(data)
  }));
}
async function inspect(page, label) {
  await page.locator('body').waitFor();
  await page.evaluate(() => document.fonts.ready);
  await page.locator('img[src]').evaluateAll(images => Promise.all(images.map(image => image.decode().catch(() => {}))));
  const state = await page.evaluate(() => ({
    width: innerWidth, scroll: document.documentElement.scrollWidth,
    broken: [...document.querySelectorAll('img[src]')].filter(image => !image.complete || !image.naturalWidth).map(image => image.src),
    overflow: [...document.querySelectorAll('h1,h2,h3,p,.button,.asset,.spec-list,.docs-content')].filter(node => {
      const rect = node.getBoundingClientRect();
      return rect.width > 0 && (rect.left < -1 || rect.right > innerWidth + 1 || node.scrollWidth > node.clientWidth + 2);
    }).map(node => node.className || node.tagName),
    h1: [...document.querySelectorAll('h1')].filter(node => node.getBoundingClientRect().height > 0).length,
    buttonContrast: [...document.querySelectorAll('.button,.nav-download')].filter(node => node.getBoundingClientRect().width > 0).map(node => {
      const style = getComputedStyle(node);
      const lum = color => {
        const values = color.match(/[\d.]+/g).slice(0, 3).map(Number).map(value => { const v=value/255; return v <= 0.04045 ? v/12.92 : ((v+0.055)/1.055)**2.4; });
        return values[0]*0.2126+values[1]*0.7152+values[2]*0.0722;
      };
      const a=lum(style.color),b=lum(style.backgroundColor);
      return (Math.max(a,b)+0.05)/(Math.min(a,b)+0.05);
    })
  }));
  check(state.scroll <= state.width + 1, `${label} document overflow ${JSON.stringify(state)}`);
  check(state.broken.length === 0, `${label} broken images ${state.broken}`);
  check(state.overflow.length === 0, `${label} clipped text ${state.overflow}`);
  check(state.h1 === 1, `${label} heading`);
  check(state.buttonContrast.every(ratio => ratio >= 4.5), `${label} button contrast ${state.buttonContrast}`);
}
(async () => {
  const executablePath = process.env.SITE_TEST_BROWSER || [
    chromium.executablePath(), 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    'C:/Program Files/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe'
  ].find(file => fs.existsSync(file));
  const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
  const pages = ['/', '/download', '/guide', '/404'];
  const matrix = [];
  try {
    for (const width of [320, 390, 768, 1024, 1440]) {
      for (const language of ['zh', 'en']) {
        const context = await browser.newContext({ viewport: { width, height: 960 }, deviceScaleFactor: width <= 768 ? 2 : 1, locale: language === 'zh' ? 'zh-CN' : 'en-US' });
        await fixture(context);
        await context.addInitScript(value => localStorage.setItem('inputstitch-lang', value), language);
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        for (const route of pages) {
          await page.goto(base + route, { waitUntil: 'networkidle' });
          check(await page.locator('html').getAttribute('lang') === (language === 'zh' ? 'zh-CN' : 'en'), `language ${width} ${language} ${route}`);
          await inspect(page, `${width} ${language} ${route}`);
          if ([390, 1440].includes(width) && route !== '/404') await page.screenshot({ path: path.join(out, `${route === '/' ? 'home' : route.slice(1)}-${language}-${width}.png`), fullPage: true });
          matrix.push({ width, language, route, deviceScaleFactor: width <= 768 ? 2 : 1 });
        }
        check(errors.length === 0, `runtime errors ${width} ${language}: ${errors}`);
        await context.close();
      }
    }
    const context = await browser.newContext({ viewport: { width: 1440, height: 960 }, locale: 'zh-CN' });
    await fixture(context);
    const page = await context.newPage();
    await page.goto(base + '/', { waitUntil: 'networkidle' });
    await page.locator('[data-lang-toggle]').click();
    check(await page.locator('html').getAttribute('lang') === 'en', 'language toggle');
    check((await page.title()).includes('Keyboard'), 'translated page title');
    check((await page.locator('.app-frame img').getAttribute('src')).includes('-en.webp'), 'translated screenshot');
    await page.goto(base + '/download', { waitUntil: 'networkidle' });
    check(await page.locator('html').getAttribute('lang') === 'en', 'language survives navigation');
    await page.goto(base + '/', { waitUntil: 'networkidle' });
    const screenshot = page.locator('[data-image-open]');
    await screenshot.focus(); await page.keyboard.press('Enter');
    check(await page.locator('[data-image-dialog]').evaluate(node => node.open), 'keyboard image enlargement');
    await page.keyboard.press('Escape');
    check(!(await page.locator('[data-image-dialog]').evaluate(node => node.open)), 'Escape closes image');
    check(await screenshot.evaluate(node => node === document.activeElement), 'dialog restores focus');
    await page.locator('[data-reticle-style="dot"]').click();
    check(await page.locator('[data-reticle] path').count() === 0 && await page.locator('[data-reticle] circle').count() === 1, 'dot preview changes geometry');
    await page.locator('[data-reticle-style="ring"]').click();
    check(await page.locator('[data-reticle] circle').count() === 2, 'ring preview');
    await page.locator('[data-reticle-color="#cbb3ff"]').click();
    check(await page.locator('[data-reticle]').evaluate(node => getComputedStyle(node).color) === 'rgb(203, 179, 255)', 'color preview');
    await page.locator('[data-reticle-opacity]').focus();
    await page.keyboard.press('Home');
    check(await page.locator('[data-opacity-value]').textContent() === '10%', 'keyboard opacity slider');
    check(await page.locator('[data-reticle]').evaluate(node => getComputedStyle(node).opacity) === '0.1', 'opacity applied');
    await page.setViewportSize({ width: 390, height: 844 });
    const menu = page.locator('[data-menu-toggle]');
    await menu.click();
    check(await menu.getAttribute('aria-expanded') === 'true', 'mobile menu open');
    await page.keyboard.press('Escape');
    check(await menu.getAttribute('aria-expanded') === 'false', 'mobile menu Escape');
    await menu.click();
    await page.locator('[data-navigation] a[href="/guide"]').click();
    check(await page.locator('body').getAttribute('data-page') === 'guide', 'mobile navigation follows link');
    await context.close();
    const blocked = await browser.newContext({ viewport: { width: 390, height: 844 }, locale: 'en-US', javaScriptEnabled: false });
    const nojs = await blocked.newPage(); await nojs.goto(base + '/download');
    check(await nojs.locator('.download-main-button').getAttribute('href') === 'https://download.zhihanyu.com/latest/InputStitch-Windows-x64.exe', 'download works without JavaScript');
    await inspect(nojs, 'no JavaScript'); await blocked.close();
    const storage = await browser.newContext({ locale: 'en-US', viewport: { width: 1024, height: 960 } });
    await fixture(storage, { ...stable, tag_name: 'v9.9.9-beta.1', prerelease: true });
    await storage.addInitScript(() => { Storage.prototype.getItem = () => { throw new Error('Storage blocked'); }; Storage.prototype.setItem = () => { throw new Error('Storage blocked'); }; });
    const fallback = await storage.newPage(); const failures = []; fallback.on('pageerror', error => failures.push(error.message));
    await fallback.goto(base + '/download', { waitUntil: 'networkidle' });
    check(await fallback.locator('html').getAttribute('lang') === 'en', 'blocked storage uses browser language');
    check(await fallback.locator('[data-release-version]').textContent() === snapshot.version, 'Beta cannot replace Stable snapshot');
    check(failures.length === 0, 'blocked preference storage does not break UI');
    await storage.close();
    const next = await browser.newContext({ locale: 'en-US' });
    await fixture(next, { ...stable, tag_name: 'v9.0.0', assets: ['x64','x86'].map(arch => ({ name: `InputStitch-9.0.0-Windows-${arch}.exe`, size: 2097152 })) });
    const latest = await next.newPage(); await latest.goto(base + '/download', { waitUntil: 'networkidle' });
    check(await latest.locator('[data-release-version]').textContent() === '9.0.0', 'official Stable metadata refresh');
    check((await latest.locator('[data-release-size="x64"]').allTextContents()).every(value => value === '2.00 MiB'), 'published size refresh in both language nodes');
    await next.close();
    const offline = await browser.newContext({ locale: 'zh-CN' });
    await offline.route('https://api.github.com/**', route => route.fulfill({ status: 503, body: '{}' }));
    const unavailable = await offline.newPage(); await unavailable.goto(base + '/download', { waitUntil: 'networkidle' });
    check(await unavailable.locator('[data-release-version]').textContent() === snapshot.version, 'unavailable external metadata preserves snapshot');
    check(await unavailable.locator('.download-main-button').getAttribute('href') === 'https://download.zhihanyu.com/latest/InputStitch-Windows-x64.exe', 'unavailable external metadata preserves website download');
    await offline.close();
    const report = { status: 'PASS', checks, matrixCases: matrix.length, matrix, browser: await browser.version(), base, scope: 'Isolated headless browser, bilingual responsive/high-DPI rendering, assets, clipping, actual keyboard/click interactions, no-JS download and unavailable preference storage; desktop runtime not tested or changed.' };
    fs.writeFileSync(path.join(out, 'browser-report.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify({ status: 'PASS', checks, matrixCases: matrix.length, browser: report.browser }));
  } finally { await browser.close(); }
})().catch(error => { fs.writeFileSync(path.join(out, 'browser-failure.txt'), String(error.stack || error)); console.error(error); process.exit(1); });
