const fs = require('fs');
const {chromium, webkit} = require(process.cwd() + '/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out = process.env.DIAPERSCOUT_BROWSER_EVIDENCE;
if (!out) throw Error('Evidence directory is required');
const report = {started: new Date().toISOString(), productionWrites: 0, physicalIphoneProven: false, syntheticTiles: true, sessions: []};
const check = (value, message) => { if (!value) throw Error(message); };
(async () => {
  for (const [name, engine, width, height, mobile] of [['chromium',chromium,390,844,true],['webkit',webkit,430,932,true],['desktop',chromium,1280,800,false]]) {
    const browser = await engine.launch({headless:true});
    const context = await browser.newContext({viewport:{width,height},isMobile:mobile,hasTouch:mobile,serviceWorkers:'block'});
    await context.addInitScript(value => Object.defineProperty(navigator,'standalone',{value}),mobile);
    await context.route('https://tile.openstreetmap.org/**', route => route.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#e8e9df"/><path d="M0 128H256M128 0V256" stroke="#c4cabc"/></svg>'}));
    const page = await context.newPage(); page.setDefaultTimeout(90000);
    const result = {engine:name,width,height,errors:[]}; report.sessions.push(result);
    page.on('pageerror', error => result.errors.push(error.message));
    try {
      const response = await page.goto('https://diaperscout.app/atlas',{waitUntil:'domcontentloaded',timeout:120000});
      check(response.status()===200,'Atlas HTTP 200');
      await page.locator('.place-map.leaflet-container').waitFor();
      await page.locator('.atlas-status').waitFor({state:'hidden'});
      check(!await page.locator('#blazor-error-ui').isVisible(),'No Blazor error');
      check(await page.locator('.atlas-category-editor').count()===0,'Anonymous has no category editor');
      result.discoveryLabel = await page.locator('.atlas-tools button').innerText();
      result.markerCount = await page.locator('.atlas-marker-art').count();
      result.geometry = await page.locator('.place-map').evaluate(e => {const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,fits:document.documentElement.scrollWidth<=innerWidth};});
      check(result.geometry.fits && result.geometry.height>height*0.65,'Usable full canvas');
      await page.locator('.leaflet-control-zoom-in').click();
      await page.locator('.place-map').press('ArrowRight');
      await page.locator('.atlas-tools button').click();
      await page.locator('#discoveries-heading').waitFor();
      result.listCount = await page.locator('.atlas-place-choice').count();
      check(result.listCount===result.markerCount,'List and qualifying pins agree');
      await page.getByRole('button',{name:'Close discoveries',exact:true}).click();
      await page.locator('#discoveries-heading').waitFor({state:'hidden'});
      if(result.markerCount===0){
        await page.locator('#uncharted-heading').waitFor();
        check(await page.locator('.atlas-field-note img').getAttribute('src')==='/pwa/guide-map.webp','Canonical Guide note');
        result.unchartedNote=true;
      }
      if(result.markerCount>0){
        await page.locator('.leaflet-marker-icon').first().press('Enter');
        await page.locator('.atlas-selected-place').waitFor();
        check((await page.locator('.atlas-stock-note').innerText()).includes('not a guarantee'),'Dated evidence wording');
        check(await page.locator('.atlas-observation a').count()>0,'Exact product links');
      }
      await page.screenshot({path:`${out}/production-atlas-${name}.png`});
      check(result.errors.length===0,'No page errors'); result.passed=true;
    } catch(error) {result.passed=false;result.failure=error.message;}
    await context.close(); await browser.close();
  }
  report.finished=new Date().toISOString();
  fs.writeFileSync(`${out}/production-smoke.json`,JSON.stringify(report,null,2));
  console.log(JSON.stringify(report,null,2));
  if(report.sessions.some(s=>!s.passed))process.exitCode=1;
})().catch(error=>{console.error(error);process.exitCode=1;});
