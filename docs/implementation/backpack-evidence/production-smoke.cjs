const fs = require('fs');
const { chromium, webkit } = require(process.cwd() + '/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out = process.env.DIAPERSCOUT_BROWSER_EVIDENCE;
if (!out) throw Error('Evidence directory required');
const report = { started: new Date().toISOString(), productionWrites: 0, authenticatedProductionChecks: false, physicalIphoneProven: false, sessions: [] };
const check = (value, message) => { if (!value) throw Error(message); };
(async () => {
  for (const [name, engine, width, height] of [['chromium', chromium, 390, 844], ['webkit', webkit, 430, 932]]) {
    const browser = await engine.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width, height }, isMobile: true, hasTouch: true, serviceWorkers: 'block' });
    await context.addInitScript(() => { Object.defineProperty(navigator, 'standalone', { value: true }); localStorage.setItem('ds-welcome-complete-v1', 'yes'); });
    const page = await context.newPage(); page.setDefaultTimeout(60000);
    const result = { engine: name, width, height, errors: [] }; report.sessions.push(result);
    page.on('pageerror', e => result.errors.push(e.message));
    try {
      const response = await page.goto('https://diaperscout.app/backpack', { waitUntil: 'domcontentloaded', timeout: 60000 });
      check(response.status() === 200, 'Backpack HTTP 200');
      await page.waitForFunction(() => document.documentElement.dataset.pwaState === 'ready');
      check(await page.locator('.backpack-guest').isVisible(), 'Guest sign-in view');
      check(await page.evaluate(() => document.querySelector('.backpack-heading').getBoundingClientRect().bottom <= document.querySelector('.backpack-guest').getBoundingClientRect().top), 'Guest heading clears sign-in card');
      check(!await page.locator('.ds-app-header').isVisible(), 'Installed header hidden');
      check(await page.locator('.pwa-mobile-nav').count() === 1, 'One navigation');
      check(await page.locator('.pwa-mobile-nav a[aria-current]').getAttribute('href') === '/backpack', 'Backpack active');
      check(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No page overflow');
      for (const path of ['/pwa/backpack-cabin.png', '/css/backpack-pwa.css', '/js/backpack-personal.js']) {
        const asset = await context.request.get('https://diaperscout.app' + path);
        check(asset.status() === 200, 'New asset ' + path);
      }
      const privateResponse = await context.request.get('https://diaperscout.app/backpack/data/account', { maxRedirects: 0 });
      result.privateStatus = privateResponse.status();
      check([302, 401, 403].includes(result.privateStatus), 'Anonymous account data denied');
      await page.screenshot({ path: `${out}/production-backpack-${name}.png`, fullPage: true });
      for (const path of ['/', '/products', '/atlas']) {
        const response = await page.goto('https://diaperscout.app' + path, { waitUntil: 'domcontentloaded', timeout: 60000 });
        check(response.status() === 200, 'Public route ' + path);
        await page.waitForFunction(() => document.documentElement.dataset.pwaState === 'ready');
        check(!await page.locator('#blazor-error-ui').isVisible(), 'Healthy route ' + path);
      }
      check(result.errors.length === 0, 'No browser errors'); result.passed = true;
    } catch (e) { result.passed = false; result.failure = e.message; }
    await context.close(); await browser.close();
  }
  report.finished = new Date().toISOString();
  fs.writeFileSync(`${out}/production-smoke.json`, JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
  if (report.sessions.some(s => !s.passed)) process.exitCode = 1;
})().catch(e => { console.error(e); process.exitCode = 1; });
