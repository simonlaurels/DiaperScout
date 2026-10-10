const fs = require('fs');
const {chromium, webkit, devices} = require(process.cwd() + '/tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package');
const report = {started:new Date().toISOString(), productionWrites:0, physicalIosProven:false, sessions:[]};
const assert = (condition,message) => { if (!condition) throw new Error(message); };
(async () => {
  for (const [engine, type] of [['chromium',chromium],['webkit',webkit]]) {
    const result={engine,errors:[],transport:[],socket:{sent:0,received:0}};
    const browser=await type.launch();
    const context=await browser.newContext({...devices['iPhone 13'],viewport:{width:390,height:844}});
    await context.addInitScript("Object.defineProperty(navigator,'standalone',{value:true});");
    const page=await context.newPage();
    page.on('pageerror',e=>result.errors.push(e.message));
    page.on('response',r=>{if(new URL(r.url()).pathname.startsWith('/_blazor'))result.transport.push({path:new URL(r.url()).pathname,status:r.status()});});
    page.on('websocket',s=>{s.on('framesent',()=>result.socket.sent++);s.on('framereceived',()=>result.socket.received++);});
    try {
      const response=await page.goto('https://diaperscout.app/products',{timeout:120000});
      assert(response.status()===200,'Products HTTP response');
      await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready',{},{timeout:90000});
      await page.locator('.catalogue-product-main h2 a').first().waitFor({timeout:90000});
      const link=page.locator('.catalogue-product-main h2 a').first();
      result.productPath=await link.getAttribute('href');
      await link.click();
      await page.locator('.product-intro h1').waitFor({timeout:90000});
      assert(!await page.locator('.ds-app-header').isVisible(),'Website header must be hidden');
      await page.evaluate(()=>document.fonts.ready);
      result.productStyle=await page.locator('.product-intro h1').evaluate(e=>({colour:getComputedStyle(e).color,weight:getComputedStyle(e).fontWeight,font:getComputedStyle(e).fontFamily}));
      assert(result.productStyle.colour==='rgb(23, 56, 65)' && result.productStyle.weight==='600','Approved heading style');
      result.productName=await page.locator('.product-intro h1').innerText();
      if(await page.locator('.gallery-counter').count()) {
        await page.waitForFunction(()=>document.querySelector('.gallery-viewport')?.dataset.galleryReady==='true');
        await page.locator('.gallery-viewport').focus();await page.keyboard.press('ArrowRight');
        result.galleryCounter=await page.locator('.gallery-counter').innerText();
      }
      result.packId=await page.locator('#selected-pack').inputValue();
      result.variantId=new URL(page.url()).searchParams.get('variantId');
      await page.locator('.product-details-card .view-all').click();
      await page.locator('.detail-expanded').waitFor();
      await page.screenshot({path:`/tmp/ds-prototype-production-product-${engine}.png`,fullPage:true});
      await page.locator('.availability-card .view-all').click();
      await page.locator('.retailer-product-meta').waitFor({timeout:90000});
      assert(new URL(page.url()).searchParams.get('packTypeId')===result.packId,'Availability preserves pack');
      assert(new URL(page.url()).searchParams.get('variantId')===result.variantId,'Availability preserves variant');
      await page.getByRole('button',{name:'All Stores',exact:true}).click();
      await page.getByRole('button',{name:'Filter',exact:true}).click();
      await page.locator('#availability-filter').fill('no-matching-retailer-smoke');
      await page.locator('.scope-empty').waitFor();
      await page.locator('#availability-filter').fill('');
      await page.getByRole('button',{name:'Filter',exact:true}).click();
      assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Availability must fit mobile width');
      await page.screenshot({path:`/tmp/ds-prototype-production-retailers-${engine}.png`,fullPage:true});
      await page.getByRole('link',{name:`Back to ${result.productName}`,exact:true}).click();
      await page.locator('#selected-pack').waitFor({timeout:90000});
      assert(await page.locator('#selected-pack').inputValue()===result.packId,'Back restores exact pack');
      await page.getByRole('link',{name:'Add Observation',exact:true}).click();
      await page.getByRole('link',{name:'Sign in and continue',exact:true}).waitFor({timeout:90000});
      assert(new URL(page.url()).searchParams.get('packTypeId')===result.packId,'Observation gate preserves pack');
      result.affinity=(await context.cookies()).filter(c=>c.name==='acaAffinity').map(({name,httpOnly,secure,sameSite})=>({name,httpOnly,secure,sameSite}));
      result.cachedPaths=await page.evaluate(async()=>{const paths=[];for(const key of await caches.keys()){for(const r of await (await caches.open(key)).keys())paths.push(new URL(r.url).pathname);}return paths;});
      assert(!await page.locator('#blazor-error-ui').isVisible(),'Blazor error banner must remain hidden');
      assert(result.errors.length===0,'Browser errors: '+result.errors.join('; '));
      assert(result.socket.received>0,'Live Blazor circuit');
      assert(result.affinity.some(c=>c.secure&&c.httpOnly),'Sticky affinity retained');
      const approvedCache = new Set(['/pwa/offline.html','/pwa/offline.css','/pwa/offline.js','/images/brand/DiaperScout.svg','/pwa/guide-map.webp','/pwa/icons/icon-192.png','/pwa/icons/icon-512.png','/pwa/icons/maskable-512.png','/pwa/icons/apple-touch-icon.png']);
      assert(result.cachedPaths.every(p=>approvedCache.has(p)),'Conservative worker cache');
      result.passed=true;
    } catch(e) {result.failure=e.message;result.passed=false;}
    finally {result.ended=new Date().toISOString();report.sessions.push(result);await context.close();await browser.close();}
  }
  report.ended=new Date().toISOString();
  fs.writeFileSync('docs/implementation/prototype-port-evidence/production-smoke.json',JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
  if(report.sessions.some(s=>!s.passed))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1;});
