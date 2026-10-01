const fs=require('fs');
const {chromium,webkit,devices}=require('/Users/sporter/Documents/Projects/DS/tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package');
(async()=>{
 const root='https://diaperscout.app',result={pages:[],checks:[]};
 const check=(name,passed)=>result.checks.push({name,passed:!!passed});
 const ready=page=>page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready',null,{timeout:45000});
 for(const [name,engine] of [['webkit',webkit],['chromium',chromium]]){
  const browser=await engine.launch();
  for(const standalone of [false,true]){
   const context=await browser.newContext({...devices['iPhone 13'],serviceWorkers:'allow'});
   await context.addInitScript(value=>Object.defineProperty(navigator,'standalone',{value}),standalone);
   await context.route('https://tile.openstreetmap.org/**',route=>route.abort()); // Never crawl public map tiles.
   const page=await context.newPage();let current;
   page.on('pageerror',error=>current?.errors.push(error.message));
   for(const path of ['/','/products','/scan','/atlas','/backpack','/contribute/product?gtin=96385074']){
    current={engine:name,standalone,path,errors:[]};result.pages.push(current);
    const response=await page.goto(root+path,{waitUntil:'domcontentloaded',timeout:60000});await ready(page);
    current.status=response.status();current.errorVisible=await page.locator('#blazor-error-ui').isVisible();
    current.fits=await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth);
    if(path==='/atlas') { await page.getByText('Loading observations…',{exact:true}).waitFor({state:'hidden',timeout:45000}); current.atlasText=(await page.locator('.atlas-page').innerText()).slice(0,1500); }
    if(path.startsWith('/contribute/'))check(name+'/'+standalone+' proposal keeps prefilled GTIN',await page.locator('.contribution-barcode').innerText()==='Scanned barcode: 96385074');
   }
   await page.goto(root+'/scan');await ready(page);
   await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();
   await page.getByRole('link',{name:'Add product',exact:true}).waitFor();
   check(name+'/'+standalone+' unknown GTIN offers proposal',true);
   await page.getByRole('link',{name:'Add product',exact:true}).click();await ready(page);
   await page.waitForURL(root+'/contribute/product?gtin=96385074');await page.locator('#proposal-brand').waitFor();
   check(name+'/'+standalone+' unknown proposal remains public',await page.locator('#proposal-brand').count()===1);
   await page.goto(root+'/products');await ready(page);
   const product=await page.locator('.catalogue-product-identity h2 a').first().getAttribute('href');
   await page.goto(root+product);await ready(page);
   const selector=page.getByRole('combobox',{name:'Pack size',exact:true});await selector.waitFor();
   const packId=await selector.inputValue();
   const exact=page.url()+'&packTypeId='+packId;await page.goto(exact);await ready(page);
   check(name+'/'+standalone+' exact pack survives URL',await selector.inputValue()===packId);
   await page.goto(root+'/observations/new?packTypeId='+packId);await ready(page);
   const signin=page.getByRole('link',{name:'Sign in and continue',exact:true});await signin.waitFor();
   check(name+'/'+standalone+' observation requires sign-in with exact pack',decodeURIComponent(await signin.getAttribute('href')).includes(packId));
   await signin.click();check(name+'/'+standalone+' existing passkey sign-in available',await page.locator('[data-passkey-signin]').count()===1);
   await context.close();
  }
  await browser.close();
 }
 check('24 public/browser/standalone pages healthy without overflow',result.pages.length===24&&result.pages.every(p=>p.status===200&&!p.errorVisible&&p.fits&&p.errors.length===0));
 fs.writeFileSync('/tmp/ds-observation-production-smoke.json',JSON.stringify(result,null,2));
 console.log(JSON.stringify({pages:result.pages.length,checks:result.checks},null,2));
 if(result.checks.some(c=>!c.passed))process.exitCode=1;
})().catch(e=>{console.error(e.message);process.exitCode=1;});
