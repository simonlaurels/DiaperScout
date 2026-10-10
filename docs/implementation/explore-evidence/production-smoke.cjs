const fs=require('fs');
const {chromium,webkit}=require(process.cwd()+'/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out=process.env.DIAPERSCOUT_BROWSER_EVIDENCE;
if(!out)throw Error('Evidence directory required');
const report={started:new Date().toISOString(),productionWrites:0,physicalIphoneProven:false,sessions:[]};
const check=(value,message)=>{if(!value)throw Error(message);};
(async()=>{
 for(const [name,engine,width,height] of [['chromium',chromium,390,844],['webkit',webkit,430,932]]){
  const browser=await engine.launch({headless:true});
  const context=await browser.newContext({viewport:{width,height},isMobile:true,hasTouch:true,serviceWorkers:'block'});
  await context.addInitScript(()=>{Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');window.locationRequests=0;Object.defineProperty(navigator,'geolocation',{value:{getCurrentPosition:(success,failure)=>{window.locationRequests++;failure({code:1});}},configurable:true});});
  const page=await context.newPage();page.setDefaultTimeout(90000);
  const result={engine:name,width,height,errors:[]};report.sessions.push(result);page.on('pageerror',e=>result.errors.push(e.message));
  try{
   const response=await page.goto('https://diaperscout.app/',{waitUntil:'domcontentloaded',timeout:120000});check(response.status()===200,'HTTP 200');
   await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');
   await page.locator('.explore-panel').first().locator('text=Finding what’s new…').waitFor({state:'hidden'});
   check(!await page.locator('.ds-app-header').isVisible(),'No installed header');
   check(await page.locator('.pwa-mobile-nav').count()===1,'Single real navigation');
   check(await page.locator('.pwa-mobile-nav a[aria-current]').getAttribute('href')==='/','Explore active');
   check(await page.evaluate(()=>window.locationRequests)===0,'No automatic geolocation');
   check(await page.locator('.explore-intro img').getAttribute('src')==='/pwa/guide-map.webp','Canonical Guide');
   check(await page.locator('.explore-panel').first().getByRole('button',{name:'Try again'}).count()===0,'Catalogue loaded');
   result.products=await page.locator('.explore-product').count();
   result.heroHeight=await page.locator('.explore-intro').evaluate(e=>e.getBoundingClientRect().height);check(result.heroHeight<=240,'Compact hero');
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'No horizontal page overflow');
   if(result.products){result.productHref=await page.locator('.explore-product').first().getAttribute('href');check(/^\/products\/[^?]+\?variantId=/.test(result.productHref),'Existing product flow');}
   await page.screenshot({path:`${out}/production-explore-${name}.png`,fullPage:true});
   await page.getByRole('button',{name:'Use my location',exact:true}).click();
   await page.locator('.explore-location').filter({hasText:'wasn’t allowed'}).waitFor();
   check(await page.evaluate(()=>window.locationRequests)===1,'Explicit denial without nagging');check(await page.locator('.explore-product').count()===result.products,'Catalogue survives denial');
   check(!await page.locator('#blazor-error-ui').isVisible(),'No application error');check(result.errors.length===0,'No page errors');
   if(result.products){await page.goto('https://diaperscout.app'+result.productHref,{timeout:120000});await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');check(!await page.locator('#blazor-error-ui').isVisible(),'Product destination healthy');result.productDestination=page.url();}
   result.passed=true;
  }catch(e){result.passed=false;result.failure=e.message;}
  await context.close();await browser.close();
 }
 report.finished=new Date().toISOString();fs.writeFileSync(`${out}/production-smoke.json`,JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(report.sessions.some(s=>!s.passed))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1;});
