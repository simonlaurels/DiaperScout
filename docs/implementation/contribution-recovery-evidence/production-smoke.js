// Browser-only unsent draft validation. Never authenticate or submit production data.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const {chromium,webkit,devices}=require(path.resolve('tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package'));
const destination=path.resolve('docs/implementation/contribution-recovery-evidence/production-smoke.json');
const result={started:new Date().toISOString(),productionSubmissions:0,productionObservations:0,authenticatedProductionTest:false,physicalIosProven:false,sessions:[]};
const save=()=>fs.writeFileSync(destination,JSON.stringify(result,null,2)+'\n');
(async()=>{for(const [name,engine] of [['chromium',chromium],['webkit',webkit]]){
 const browser=await engine.launch();const context=await browser.newContext({...devices['iPhone 13'],serviceWorkers:'allow'});
 await context.addInitScript(()=>Object.defineProperty(navigator,'standalone',{value:true}));
 const page=await context.newPage();const session={engine:name,errors:[],transport:[],socket:{sent:0,received:0},steps:[]};result.sessions.push(session);save();
 page.on('pageerror',()=>session.errors.push('Page script error'));
 page.on('response',r=>{if(new URL(r.url()).pathname.startsWith('/_blazor'))session.transport.push({path:new URL(r.url()).pathname,status:r.status()});});
 page.on('websocket',ws=>{if(new URL(ws.url()).pathname==='/_blazor'){ws.on('framesent',()=>session.socket.sent++);ws.on('framereceived',()=>session.socket.received++);ws.on('socketerror',()=>session.errors.push('Circuit transport error'));}});
 const ready=()=>page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready',null,{timeout:60000});
 const healthy=async()=>{if(await page.locator('#blazor-error-ui').isVisible())throw Error('Blazor failure banner');};
 try {
  const response=await page.goto('https://diaperscout.app/scan',{timeout:90000,waitUntil:'domcontentloaded'});session.http=response.status();await ready();await healthy();
  await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();
  await page.getByRole('link',{name:'Add product',exact:true}).waitFor({timeout:60000});await page.getByRole('link',{name:'Add product',exact:true}).click();
  await page.locator('#proposal-brand').fill('Unsubmitted local smoke');await page.locator('#proposal-name').fill('Browser only');await page.locator('#proposal-manufacturer').fill('Local draft');
  await page.getByRole('button',{name:'Continue to pack',exact:true}).click();await page.locator('#proposal-size').fill('S');await page.locator('#proposal-quantity').fill('12');
  await page.getByRole('button',{name:'Review proposal',exact:true}).click();await page.locator('.contribution-review').waitFor();
  const draft=await page.evaluate(()=>JSON.parse(localStorage.getItem('diaperscout-product-draft:96385074')));
  if(draft.step!==3||draft.draft.quantity!==12)throw Error('Draft not saved at Review');
  session.steps.push('Unknown lookup and Product/Pack/Review on live circuit');
  await page.waitForTimeout(35000);await page.getByRole('button',{name:'Edit information',exact:true}).click();await page.getByRole('button',{name:'Continue to pack',exact:true}).click();await page.getByRole('button',{name:'Review proposal',exact:true}).click();await healthy();
  session.steps.push('Interactive after 35 seconds');
  await page.getByRole('button',{name:'Sign in to submit',exact:true}).click();await page.waitForURL('**/signin?returnUrl=**');
  await page.locator('[data-passkey-signin]').waitFor();session.steps.push('Passkey sign-in with contribution return URL; no authentication performed');
  const next=await context.newPage();await next.goto('https://diaperscout.app/contribute/product?gtin=96385074',{timeout:90000});
  await next.locator('.contribution-review').waitFor({timeout:60000});
  const restored=await next.evaluate(()=>JSON.parse(localStorage.getItem('diaperscout-product-draft:96385074')));
  if(JSON.stringify(restored.draft)!==JSON.stringify(draft.draft)||restored.step!==3)throw Error('New tab lost exact draft');
  session.draftHash=crypto.createHash('sha256').update(JSON.stringify(restored.draft)).digest('hex');session.steps.push('Exact draft and stable contribution ID restored at Review in new tab');
  await next.reload({timeout:90000});await next.locator('.contribution-review').waitFor({timeout:60000});session.steps.push('Review restored after full reload');
  session.affinity=(await context.cookies()).filter(c=>/affinity/i.test(c.name)).map(c=>({name:c.name,httpOnly:c.httpOnly,secure:c.secure,sameSite:c.sameSite}));
  if(!session.affinity.length)throw Error('Sticky cookie absent');
  session.cachedPaths=await next.evaluate(async()=>{const paths=[];for(const n of await caches.keys())for(const r of await(await caches.open(n)).keys())paths.push(new URL(r.url).pathname);return paths;});
  if(session.cachedPaths.some(p=>!p.startsWith('/pwa/')&&p!='/images/brand/DiaperScout.svg'))throw Error('Application data cached');
  if(session.socket.sent<4||session.socket.received<4||session.transport.some(t=>t.status>=400)||session.errors.length)throw Error('Circuit validation failed');
  await next.evaluate(()=>localStorage.removeItem('diaperscout-product-draft:96385074'));
  session.passed=true;
 }catch(e){session.passed=false;session.failure=e.message;process.exitCode=1;}
 finally{session.ended=new Date().toISOString();save();console.log(JSON.stringify(session));await context.close();await browser.close();}
}result.ended=new Date().toISOString();save();})().catch(e=>{result.failure=e.message;save();process.exitCode=1;});
