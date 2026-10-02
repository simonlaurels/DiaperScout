// Read-only browser interactions. Never submit a proposal/observation or authenticate.
const fs=require('fs'), crypto=require('crypto'), path=require('path');
const packagePath=path.resolve('tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package');
const {chromium,webkit,devices,expect}=require(packagePath);
const phase=process.argv[2];
if(!['two-replicas','one-replica'].includes(phase)) throw new Error('Explicit phase required');
const out=path.resolve(`docs/implementation/pwa-reliability-evidence/production-${phase}.json`);
const root='https://diaperscout.app';
const result={phase,started:new Date().toISOString(),sessions:[],productionWrites:0,physicalIosProven:false};
const save=()=>fs.writeFileSync(out,JSON.stringify(result,null,2)+'\n');
const clean=message=>String(message).replace(/([?&]id=)[^\s'"&]+/g,'$1[redacted]');
const ready=page=>page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready',null,{timeout:45000});
async function assertHealthy(page){await expect(page.locator('#blazor-error-ui')).toBeHidden();await expect(page.locator('#pwa-startup')).toBeHidden();}
function descriptorKeys(html){
 const ids=[];
 for(const match of html.matchAll(/<!--Blazor:({[^]*?})-->/g)){
  try{const data=JSON.parse(match[1]);if(!data.descriptor)continue; const b=Buffer.from(data.descriptor,'base64');
   if(b.subarray(0,4).toString('hex')!=='09f0c9f0')continue;
   const id=[b.subarray(4,8).reverse().toString('hex'),b.subarray(8,10).reverse().toString('hex'),b.subarray(10,12).reverse().toString('hex'),b.subarray(12,14).toString('hex'),b.subarray(14,20).toString('hex')].join('-');ids.push(id);
  }catch{}
 }
 return [...new Set(ids)];
}
(async()=>{
 for(let round=1;round<=2;round++)for(const [engineName,engine] of [['chromium',chromium],['webkit',webkit]]){
  const browser=await engine.launch();
  for(const standalone of [false,true]){
   const session={round,engine:engineName,standalone,started:new Date().toISOString(),steps:[],errors:[],consoleErrors:[],transport:[],webSockets:[],finished:false};
   result.sessions.push(session);save();
   const context=await browser.newContext({...devices['iPhone 13'],serviceWorkers:'allow'});
   await context.addInitScript(v=>Object.defineProperty(navigator,'standalone',{value:v}),standalone);
   await context.route('https://tile.openstreetmap.org/**',r=>r.abort());
   // Defense in depth: disallow every application write endpoint during the smoke.
   await context.route('**/api/**',async r=>{
    const q=r.request();if(!['GET','HEAD','OPTIONS'].includes(q.method())){session.errors.push('Unexpected client API write blocked: '+new URL(q.url()).pathname);await r.abort();}else await r.continue();
   });
   const page=await context.newPage();
   page.on('pageerror',e=>session.errors.push(clean(e.message)));
   page.on('console',m=>{if(m.type()==='error')session.consoleErrors.push(clean(m.text()));});
   page.on('response',r=>{const u=new URL(r.url());if(u.pathname.startsWith('/_blazor'))session.transport.push({at:new Date().toISOString(),path:u.pathname,status:r.status()});});
   page.on('websocket',ws=>{const record={path:new URL(ws.url()).pathname,opened:new Date().toISOString(),received:0,sent:0};session.webSockets.push(record);
    ws.on('framereceived',()=>record.received++);ws.on('framesent',()=>record.sent++);ws.on('socketerror',e=>record.error=clean(e));ws.on('close',()=>record.closed=new Date().toISOString());
   });
   try{
    const response=await page.goto(root,{waitUntil:'domcontentloaded',timeout:60000});if(response.status()!==200)throw new Error('Explore HTTP '+response.status());await ready(page);await assertHealthy(page);session.steps.push('Explore ready');
    await page.locator('.pwa-mobile-nav a[href="/scan"]').click();
    await expect(page.locator('#gtin')).toBeEnabled({timeout:45000});await ready(page);await assertHealthy(page);session.steps.push('Scan interactive');
    await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();
    await expect(page.getByRole('heading',{name:'We don’t have this one yet.',exact:true})).toBeVisible({timeout:45000});session.steps.push('Unknown barcode lookup completed');
    await page.getByRole('link',{name:'Add product',exact:true}).click();await expect(page.locator('#proposal-brand')).toBeVisible({timeout:45000});
    await expect(page.locator('.contribution-barcode')).toContainText('96385074');await assertHealthy(page);
    // These values exist only in this browser's unsent form/draft.
    await page.locator('#proposal-brand').fill('Unsubmitted reliability check');await page.locator('#proposal-name').fill('Local form only');
    await page.getByRole('button',{name:'Continue to pack',exact:true}).click();await expect(page.locator('#proposal-size')).toBeVisible();
    session.steps.push('Proposal step transition handled by live circuit');
    const descriptorResponse=await context.request.get(root+'/scan');
    if(descriptorResponse.status()!==200)throw new Error('SSR descriptor response unavailable');
    session.descriptorKeyIds=descriptorKeys(await descriptorResponse.text());
    if(session.descriptorKeyIds.length!==1)throw new Error('Shared key descriptor could not be verified');
    const before=Date.now();await page.waitForTimeout(35000);session.circuitDwellMs=Date.now()-before;
    await page.locator('#proposal-size').fill('Large');await page.locator('#proposal-quantity').fill('12');
    await page.getByRole('button',{name:'Review proposal',exact:true}).click();await expect(page.locator('.contribution-review')).toBeVisible();await assertHealthy(page);
    session.steps.push('Circuit remained interactive after 35 seconds/keepalives');
    await page.getByRole('button',{name:'Sign in to submit',exact:true}).click();
    await expect(page.locator('[data-passkey-signin]')).toBeVisible({timeout:45000});session.steps.push('Existing passkey sign-in reached; no authentication or submission');
    session.authReturnPreserved=decodeURIComponent(page.url()).includes('/contribute/product?gtin=96385074');
    if(!session.authReturnPreserved)throw new Error('Contribution return URL was lost');
    await page.goto(root+'/atlas',{waitUntil:'domcontentloaded',timeout:60000});await ready(page);
    await expect(page.getByText('Loading observations…',{exact:true})).toBeHidden({timeout:45000});
    await expect(page.locator('.atlas-page')).toBeVisible();await expect(page.locator('.atlas-page [role="alert"]')).toHaveCount(0);await assertHealthy(page);session.steps.push('Atlas loaded');
    await page.locator('.pwa-mobile-nav a[href="/scan"]').click();await expect(page.locator('#gtin')).toBeEnabled({timeout:45000});
    await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();
    await expect(page.getByRole('link',{name:'Add product',exact:true})).toBeVisible({timeout:45000});await assertHealthy(page);session.steps.push('Scan lookup still interactive after Atlas navigation');
    const cookies=(await context.cookies()).filter(c=>/affinity/i.test(c.name));
    session.affinityCookies=cookies.map(c=>({name:c.name,secure:c.secure,httpOnly:c.httpOnly,sameSite:c.sameSite,replicaGroupHash:crypto.createHash('sha256').update(c.value).digest('hex').slice(0,12)}));
    if(!cookies.length)throw new Error('ACA affinity cookie absent');
    const cached=await page.evaluate(async()=>{const out=[];for(const k of await caches.keys())for(const r of await(await caches.open(k)).keys())out.push(new URL(r.url).pathname);return out;});
    session.cachedPaths=cached;
    if(cached.some(u=>!u.startsWith('/pwa/')&&u!='/images/brand/DiaperScout.svg'))throw new Error('Dynamic/application response cached');
    if(!session.webSockets.some(w=>w.path==='/_blazor'&&w.received>3&&w.sent>3))throw new Error('No usable interactive WebSocket evidence');
    if(session.errors.length||session.transport.some(r=>r.status>=400)||session.webSockets.some(w=>w.error))throw new Error('Browser/circuit failure recorded');
    session.finished=true;
   }catch(e){session.failure=clean(e.message);process.exitCode=1;}
   finally{session.ended=new Date().toISOString();save();console.log(JSON.stringify({phase,round,engine:engineName,standalone,passed:session.finished,failure:session.failure,transport:session.transport,webSockets:session.webSockets,affinity:session.affinityCookies}));await context.close();save();}
  }
  await browser.close();
 }
 result.ended=new Date().toISOString();result.passed=result.sessions.length===8&&result.sessions.every(s=>s.finished);save();
 if(!result.passed)process.exitCode=1;
})().catch(e=>{result.fatal=clean(e.message);save();console.error(result.fatal);process.exitCode=1;});
