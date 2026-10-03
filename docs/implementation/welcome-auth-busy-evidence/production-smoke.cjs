const fs = require('fs');
const { chromium, webkit } = require(process.cwd() + '/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out = 'docs/implementation/welcome-auth-busy-evidence';
const report = { started: new Date().toISOString(), productionAuthWrites: 0, physicalIphoneProven: false, sessions: [] };
const check = (value, message) => { if (!value) throw Error(message); };
const wait = promise => { let timer; return Promise.race([promise, new Promise((_, reject) => timer = setTimeout(() => reject(Error('Expected request did not start')), 30000))]).finally(() => clearTimeout(timer)); };
const bounds = page => page.evaluate(() => [...document.querySelectorAll('.welcome-primary,.welcome-secondary,.welcome-card')].map(e => { const r = e.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }));
(async () => {
    for (const [name, type, width, height] of [['chromium', chromium, 390, 844], ['webkit', webkit, 375, 667]]) {
        const browser = await type.launch({ headless: true });
        const context = await browser.newContext({ viewport: { width, height }, isMobile: true, hasTouch: true, serviceWorkers: 'block' });
        await context.addInitScript("Object.defineProperty(navigator,'standalone',{value:true});Object.defineProperty(window,'PublicKeyCredential',{value:function(){},configurable:true});Object.defineProperty(navigator,'credentials',{value:{get:async()=>{throw new DOMException('Cancelled','NotAllowedError');}},configurable:true});");
        const page = await context.newPage(); page.setDefaultTimeout(90000);
        const result = { engine: name, width, height, errors: [], deterministicColdRequest: true };
        page.on('pageerror', e => result.errors.push(e.message));
        let optionsCount = 0, joinCount = 0, releaseOptions, releaseJoin, notifyOptions, notifyJoin;
        const optionsGate = new Promise(resolve => releaseOptions = resolve);
        const joinGate = new Promise(resolve => releaseJoin = resolve);
        const optionsStarted = new Promise(resolve => notifyOptions = resolve);
        const joinStarted = new Promise(resolve => notifyJoin = resolve);
        await page.route('**/signin/passkey/options', async route => { optionsCount++; notifyOptions(); await optionsGate; await route.fulfill({ status: 503, contentType: 'application/json', body: '{"message":"Please try again."}' }); });
        await page.route('**/join', async route => { joinCount++; notifyJoin(); await joinGate; await route.continue(); });
        try {
            await page.goto('https://diaperscout.app/', { timeout: 120000 });
            await page.locator('#pwa-welcome').waitFor({ state: 'visible' });
            check(await page.locator('.welcome-secondary').innerText() === 'Sign in', 'Normal Sign in');
            check(await page.locator('.welcome-primary').innerText() === 'Create account', 'Normal Create account');
            check(await page.locator('.welcome-art').getAttribute('src') === '/pwa/welcome-portrait.webp', 'Canonical artwork');
            const before = await bounds(page);
            await page.locator('.welcome-secondary').press('Enter');
            await page.waitForFunction(() => document.querySelector('.welcome-secondary')?.getAttribute('aria-busy') === 'true');
            check(await page.locator('.welcome-secondary').innerText() === 'Signing in…', 'Immediate Sign in label');
            check(await page.locator('.welcome-auth-spinner').getAttribute('aria-hidden') === 'true', 'Decorative spinner');
            await page.evaluate(() => { document.querySelector('.welcome-secondary').dispatchEvent(new MouseEvent('click', { bubbles: true })); document.querySelector('.welcome-primary').click(); });
            await wait(optionsStarted);
            check(JSON.stringify(before) === JSON.stringify(await bounds(page)), 'No Sign in layout movement');
            check(optionsCount === 1 && joinCount === 0, 'No duplicate/conflicting auth request');
            await page.screenshot({ path: `${out}/signin-busy-${name}.png` });
            releaseOptions();
            await page.waitForFunction(() => document.querySelector('.welcome-secondary')?.textContent === 'Sign in' && !document.querySelector('.welcome-secondary').disabled);
            check(await page.locator('.welcome-signin [data-passkey-message]').innerText() === 'Please try again.', 'Existing failure handling');
            await page.locator('[data-passkey-fallback]').click();
            await page.waitForURL('**/signin'); await page.locator("form[action='/signin/request'] input[type='email']").waitFor();
            result.signin = { immediateFeedback: true, repeatedActivationPrevented: true, errorRestored: true, existingEmailDestination: '/signin', layoutStable: true };
            await page.goto('https://diaperscout.app/'); await page.locator('#pwa-welcome').waitFor({ state: 'visible' });
            const createBefore = await bounds(page);
            const click = page.locator('.welcome-primary').click();
            click.catch(() => {});
            await page.waitForFunction(() => document.querySelector('.welcome-primary')?.getAttribute('aria-busy') === 'true');
            check(await page.locator('.welcome-primary').innerText() === 'Getting ready…', 'Immediate account label');
            await page.evaluate(() => { document.querySelector('.welcome-primary').click(); document.querySelector('.welcome-secondary').click(); });
            await wait(joinStarted);
            check(JSON.stringify(createBefore) === JSON.stringify(await bounds(page)), 'No account layout movement');
            check(joinCount === 1 && optionsCount === 1, 'No conflicting registration request');
            await page.screenshot({ path: `${out}/join-busy-${name}.png` });
            releaseJoin(); await click; await page.waitForURL('**/join');
            await page.goBack(); await page.locator('#pwa-welcome').waitFor({ state: 'visible' });await page.waitForFunction(()=>document.querySelector('.welcome-primary')?.textContent==='Create account' && !document.querySelector('.welcome-primary').hasAttribute('aria-busy'),{},{timeout:10000});
            check(await page.locator('.welcome-primary').innerText() === 'Create account', 'Revisit clears busy state');
            result.createAccount = { immediateFeedback: true, existingDestination: '/join', repeatedActivationPrevented: true, layoutStable: true, revisitReset: true };
            await page.locator('#welcome-continue').click(); await page.locator('#pwa-welcome').waitFor({ state: 'hidden' });
            check(await page.evaluate(() => localStorage.getItem('ds-welcome-complete-v1')) === 'yes', 'Guest persistence');
            check(result.errors.length === 0, 'No browser errors'); result.passed = true;
        } catch (e) { result.failure = e.message; result.passed = false; }
        finally { releaseOptions(); releaseJoin(); result.ended = new Date().toISOString(); report.sessions.push(result); console.log(JSON.stringify(result)); await context.close(); await browser.close(); }
    }
    report.ended = new Date().toISOString(); fs.writeFileSync(`${out}/production-smoke.json`, JSON.stringify(report, null, 2) + '\n');
    if (report.sessions.some(s => !s.passed)) process.exitCode = 1;
})().catch(e => { console.error(e); process.exitCode = 1; });
