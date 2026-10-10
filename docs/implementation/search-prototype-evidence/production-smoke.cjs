const fs = require('fs');
const crypto = require('crypto');
const {chromium,webkit} = require(process.cwd()+'/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out='docs/implementation/search-prototype-evidence';
const report={started:new Date().toISOString(),productionWrites:0,physicalIosProven:false,sessions:[]};
const check=(v,m)=>{if(!v)throw Error(m)};
(async()=>{
 for(const [name,type,width] of [['chromium',chromium,390],['webkit',webkit,430]]){
  const browser=await type.launch({headless:true}); const context=await browser.newContext({viewport:{width,height:844},isMobile:true,hasTouch:true});
  await context.addInitScript("Object.defineProperty(navigator,'standalone',{value:true});");
  const page=await context.newPage();page.setDefaultTimeout(90000);const result={engine:name,width,errors:[]};page.on('pageerror',e=>result.errors.push(e.message));
  try{
   const res=await page.goto('https://diaperscout.app/products',{timeout:120000});check(res.status()===200,'Search HTTP');
   await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');
   const input=page.locator('#pwa-search-query');await input.waitFor();await page.waitForFunction(()=>!document.querySelector('#pwa-search-query').disabled);
   await page.evaluate(()=>document.fonts.ready);check(!await page.locator('.ds-app-header').isVisible(),'Website header hidden');
   result.headingStyle=await page.locator('.search-header h1').evaluate(e=>({font:getComputedStyle(e).fontFamily,size:getComputedStyle(e).fontSize}));check(result.headingStyle.font.startsWith('Inter'),'Inter font');
   check(await page.locator('.category-item').count()===5,'Five categories');
   result.assetHashesMatch=true;for(const asset of JSON.parse(fs.readFileSync(out+'/reference/asset-hashes.json','utf8'))){const r=await context.request.get('https://diaperscout.app/images/search/'+asset.asset);check(r.ok(),'Asset HTTP');check(crypto.createHash('sha256').update(await r.body()).digest('hex')===asset.sha256,'Exact prototype asset '+asset.asset);}check(await page.locator('.category-item img').evaluateAll(es=>es.every(e=>e.complete&&e.naturalWidth>0)),'Category images');
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Search width');await page.screenshot({path:out+'/production-browse-'+name+'.png',fullPage:true});
   await input.fill('MEGAMAX');await input.press('Enter');const card=page.locator('.search-result-card').filter({hasText:'MEGAMAX Black'}).first();await card.waitFor();
   await page.waitForFunction(()=>{const names=[...document.querySelectorAll('.search-result-copy strong')];return names.length>0&&names.every(e=>/MEGAMAX/i.test(e.textContent));});
   result.resultCount=await page.locator('.search-result-card').count();result.productPath=await card.getAttribute('href');check(result.productPath.includes('variantId='),'Canonical variant link');
   await page.screenshot({path:out+'/production-results-'+name+'.png',fullPage:true});await card.click();await page.locator('.product-intro h1').waitFor();
   result.productName=await page.locator('.product-intro h1').innerText();
   result.variantId=new URL(page.url()).searchParams.get('variantId');result.packId=await page.locator('#selected-pack').inputValue();
   await page.locator('.availability-card .view-all').click();await page.locator('.retailer-product-meta').waitFor();
   check(new URL(page.url()).searchParams.get('variantId')===result.variantId,'Available In variant');check(new URL(page.url()).searchParams.get('packTypeId')===result.packId,'Available In pack');result.availablePath=new URL(page.url()).pathname+new URL(page.url()).search;
   await page.goBack();await page.locator('.product-intro h1').waitFor();await page.goBack();await input.waitFor();check(await input.inputValue()==='MEGAMAX','Back retains query');
   await page.locator('.category-item').first().click();await page.waitForFunction(()=>new URL(location.href).searchParams.getAll('productType').includes('Tape'));
   await page.getByRole('button',{name:'Filter search',exact:true}).click();await page.locator('#pwa-search-filters').waitFor();check(await page.locator('#pwa-search-filters fieldset').count()>0,'Real facets');
   await page.getByRole('button',{name:'Filter search',exact:true}).click();await input.fill('no-production-search-match-98f121');await input.press('Enter');await page.getByRole('heading',{name:'No products found',exact:true}).waitFor();
   await page.locator('.pwa-mobile-nav a[href="/scan"]').click();await page.waitForURL('**/scan');check(!await page.locator('#blazor-error-ui').isVisible(),'No circuit error');check(result.errors.length===0,'No browser errors');result.passed=true;
  }catch(e){result.passed=false;result.failure=e.message;}
  finally{result.ended=new Date().toISOString();report.sessions.push(result);console.log(JSON.stringify(result));await context.close();await browser.close();}
 }
 report.ended=new Date().toISOString();fs.writeFileSync(out+'/production-smoke.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(report.sessions.some(s=>!s.passed))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1});
