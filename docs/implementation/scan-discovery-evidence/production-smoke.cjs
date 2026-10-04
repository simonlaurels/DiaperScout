const fs=require('fs');
const {chromium,webkit}=require(process.cwd()+'/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out=process.env.DIAPERSCOUT_BROWSER_EVIDENCE;
if(!out)throw Error('Evidence directory required');
const report={started:new Date().toISOString(),productionWrites:0,authenticatedProductionChecks:false,physicalIphoneProven:false,sessions:[]};
const check=(ok,message)=>{if(!ok)throw Error(message)};
(async()=>{
for(const [name,engine,width,height] of [['chromium',chromium,390,844],['webkit',webkit,430,932]]){
const browser=await engine.launch({headless:true});
const context=await browser.newContext({viewport:{width,height},isMobile:true,hasTouch:true,serviceWorkers:'block'});
await context.addInitScript(()=>{Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes')});
await context.route('https://tile.openstreetmap.org/**',r=>r.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#e8e9df"/></svg>'}));
const page=await context.newPage();page.setDefaultTimeout(45000);
const result={engine:name,width,height,errors:[],routes:[]};report.sessions.push(result);
page.on('pageerror',e=>result.errors.push(e.message));
try{
for(const route of ['/','/products','/atlas','/backpack','/scan']){
const response=await page.goto('https://diaperscout.app'+route,{waitUntil:'domcontentloaded',timeout:45000});
check(response.status()===200,'Public route '+route);await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');
check(!await page.locator('#blazor-error-ui').isVisible(),'Healthy route '+route);
await page.waitForLoadState('networkidle');result.routes.push({route,status:response.status()});
}
await page.getByRole('button',{name:'Enter barcode number',exact:true}).click();
await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();
await page.getByRole('heading',{name:'We don’t recognise this barcode yet.',exact:true}).waitFor();
check(!await page.locator('.ds-app-header').isVisible(),'Installed scanner header hidden');
await page.screenshot({path:out+'/production-unknown-'+name+'.png',fullPage:true});
await page.getByRole('link',{name:'Add this product',exact:true}).click();
await page.getByRole('heading',{name:'Take photos of the pack',exact:true}).waitFor();
await page.getByRole('button',{name:'Sign in and continue',exact:true}).waitFor();
check(await page.locator('.pwa-mobile-nav a[aria-current]').getAttribute('href')==='/scan','Scan context active');
check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'No overflow');
await page.screenshot({path:out+'/production-photo-entry-'+name+'.png',fullPage:true});
await page.getByRole('button',{name:'Sign in and continue',exact:true}).click();
await page.waitForURL('**/signin?returnUrl=**');
check(decodeURIComponent(new URL(page.url()).searchParams.get('returnUrl')).includes('gtin=96385074'),'Barcode preserved for authentication');
const privateImage=await context.request.get('https://diaperscout.app/contribute/product/images/00000000-0000-0000-0000-000000000001/00000000-0000-0000-0000-000000000002',{maxRedirects:0});
result.privateImageStatus=privateImage.status();check([302,401,403].includes(result.privateImageStatus),'Private evidence denies anonymous access');
check(result.errors.length===0,'No browser errors: '+result.errors.join('; '));result.passed=true;
}catch(e){result.failure=e.message;result.passed=false}
await browser.close();fs.writeFileSync(out+'/production-smoke.json',JSON.stringify(report,null,2));
}
report.finished=new Date().toISOString();fs.writeFileSync(out+'/production-smoke.json',JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(report.sessions.some(x=>!x.passed))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1});

