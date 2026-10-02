const { chromium, webkit } = require(process.cwd() + '/tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package');
const assert = require('node:assert/strict');
(async () => {
 for (const [engine, type] of [['chromium', chromium], ['webkit', webkit]]) {
  const browser = await type.launch({headless:true});
  const context = await browser.newContext({viewport:{width:390,height:844}});
  await context.addInitScript(() => Object.defineProperty(navigator,'standalone',{value:true}));
  const page = await context.newPage();
  await page.goto('https://diaperscout.app/');
  await page.locator('#pwa-welcome:not([hidden])').waitFor({timeout:90000});
  assert.equal(await page.evaluate(() => document.documentElement.dataset.pwaState),'ready');
  assert.equal(await page.locator('meta[name="apple-mobile-web-app-status-bar-style"]').getAttribute('content'),'black-translucent');
  assert.equal(await page.locator('.welcome-art').count(),1);
  assert.equal(await page.locator('.welcome-brand').count(),0);
  const background=await page.locator('#pwa-welcome').evaluate(el=>getComputedStyle(el).backgroundImage);
  assert.equal(background,'none');
  assert.equal(await page.locator('.welcome-art').getAttribute('src'),'/pwa/welcome-portrait.webp');
  assert.equal(await page.locator('.welcome-art').evaluate(el=>getComputedStyle(el).objectFit),'cover');
  for (const selector of ['.welcome-primary','.welcome-secondary','#welcome-continue']) {
   const box=await page.locator(selector).boundingBox(); assert(box && box.y>=0 && box.y+box.height<=844);
  }
  await page.screenshot({path:`/tmp/ds-welcome-production/${engine}-welcome.png`});
  assert.equal(await page.locator('.welcome-secondary').getAttribute('data-passkey-action'),'signin');
  await page.goto('https://diaperscout.app/signin');
  assert.equal(await page.locator('[data-passkey-action="signin"]').innerText(),'Sign in');
  assert.equal(await page.locator('form[action="/signin/request"]').count(),1);
  await page.goto('https://diaperscout.app/'); await page.locator('#welcome-continue').click();
  await page.locator('.explore-page').waitFor(); await page.reload();
  await page.waitForFunction(() => document.documentElement.dataset.pwaState==='ready');
  assert.equal(await page.locator('#pwa-welcome').isVisible(),false);
  await page.goto('https://diaperscout.app/scan');
  await page.waitForFunction(() => document.documentElement.dataset.pwaState==='ready');
  assert.equal(await page.locator('#pwa-welcome').count(),0);
  assert.equal(await page.locator('#blazor-error-ui').isVisible(),false);
  console.log(JSON.stringify({engine,at:new Date().toISOString(),welcome:true,signInLabel:'Sign in',guestPersistence:true,anonymousScan:true,productionWrites:0}));
  await browser.close();
 }
})().catch(e=>{console.error(e);process.exitCode=1;});
