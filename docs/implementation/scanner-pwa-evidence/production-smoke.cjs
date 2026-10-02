const fs=require('fs');const {chromium,webkit}=require(process.cwd()+'/tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/.playwright/package');
const out='docs/implementation/scanner-pwa-evidence';const report={started:new Date().toISOString(),productionWrites:0,physicalIosProven:false,sessions:[]};const check=(value,message)=>{if(!value)throw Error(message)};
(async()=>{
 for(const [name,type,width,height] of [['chromium',chromium,390,844],['webkit',webkit,430,932],['webkit-landscape',webkit,844,390]]){
  const browser=await type.launch({headless:true,...(name==='chromium'?{args:['--use-fake-ui-for-media-stream','--use-fake-device-for-media-stream']}: {})});
  const context=await browser.newContext({viewport:{width,height},isMobile:true,hasTouch:true});
  await context.addInitScript("Object.defineProperty(navigator,'standalone',{value:true});");
  if(name==='chromium')await context.addInitScript("window.scannerTracks=[];window.testGtin=null;window.cameraRequests=0;const get=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);navigator.mediaDevices.getUserMedia=async c=>{cameraRequests++;const stream=await get(c);scannerTracks.push(...stream.getTracks());return stream;};window.BarcodeDetector=class{static async getSupportedFormats(){return ['ean_13','ean_8','upc_a'];}async detect(){return window.testGtin?[{rawValue:testGtin,format:'ean_13'},{rawValue:testGtin,format:'ean_13'}]:[];}};");
  else await context.addInitScript("Object.defineProperty(navigator,'mediaDevices',{value:{getUserMedia:async()=>{throw new DOMException('Permission test','NotAllowedError');}},configurable:true});");
  const page=await context.newPage();page.setDefaultTimeout(90000);const result={engine:name,width,height,errors:[]};page.on('pageerror',e=>result.errors.push(e.message));
  try{
   let knownGtin=null;
   if(name==='chromium'){
    await page.goto('https://diaperscout.app/products/megamax?variantId=bb3ad9fc-9dea-4f07-b179-5bc3f66b7d7e',{timeout:120000});await page.locator('.product-intro h1').waitFor();await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');
    await page.locator('.product-details-card .view-all').click();await page.locator('.detail-expanded').waitFor();
    const gtinText=await page.locator('.detail-expanded dl div').filter({has:page.locator('dt').filter({hasText:/^GTIN$/})}).locator('dd').innerText();knownGtin=gtinText.match(/\b\d{8,14}\b/)?.[0]||null;result.publicGtin=knownGtin;
   }
   const response=await page.goto('https://diaperscout.app/scan',{timeout:120000});check(response.status()===200,'Scan HTTP');await page.waitForFunction(()=>document.documentElement.dataset.pwaState==='ready');
   check(await page.locator('.pwa-mobile-nav').isVisible(),'Existing navigation visible');check(await page.locator('.pwa-mobile-nav a[href="/scan"]').getAttribute('aria-current')==='page','Scan selected');check(!await page.locator('.ds-app-header').isVisible(),'Website header hidden');
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'No horizontal overflow');
   result.geometry=await page.evaluate(()=>{const v=document.querySelector('video').getBoundingClientRect(),n=document.querySelector('.pwa-mobile-nav').getBoundingClientRect();return {videoTop:v.top,videoWidth:v.width,videoBottom:v.bottom,navTop:n.top,navHeight:n.height};});check(Math.abs(result.geometry.videoBottom-result.geometry.navTop)<=1&&result.geometry.videoTop===0&&result.geometry.videoWidth===width,'Camera owns viewport above nav');
   if(name==='chromium'){
    await page.waitForFunction(()=>document.querySelector('.scan-page').dataset.cameraReady==='true');await page.waitForFunction(()=>document.querySelector('video').videoWidth>0);
    result.actualBrowserMediaStream=true;check(await page.getByRole('button',{name:'Toggle flashlight',exact:true}).count()===0,'Unsupported fake-camera torch absent');
    await page.screenshot({path:out+'/production-camera-chromium.png'});
    if(knownGtin){await page.evaluate(code=>window.testGtin=code,knownGtin);await page.locator('.scan-result-found').waitFor();result.exactProductPath=await page.getByRole('link',{name:'View product',exact:true}).getAttribute('href');check(result.exactProductPath.includes('variantId=')&&result.exactProductPath.includes('packTypeId='),'Known barcode keeps exact identity');await page.waitForFunction(()=>scannerTracks.every(t=>t.readyState==='ended'));await page.evaluate(()=>window.testGtin=null);await page.getByRole('button',{name:'Scan another item',exact:true}).click();await page.waitForFunction(()=>document.querySelector('.scan-page').dataset.cameraReady==='true');}
    else result.knownProductionCapture='No GTIN recorded on inspected public pack; local real-decoder/known-pack tests cover it.';
    await page.evaluate(()=>{Object.defineProperty(document,'hidden',{value:true,configurable:true});document.dispatchEvent(new Event('visibilitychange'));Object.defineProperty(document,'hidden',{value:false,configurable:true});document.dispatchEvent(new Event('visibilitychange'));});
    await page.locator('.scanner-state').filter({hasText:'app was away'}).waitFor();await page.waitForFunction(()=>scannerTracks.every(t=>t.readyState==='ended'));
    await page.getByRole('button',{name:'Try camera again',exact:true}).click();await page.waitForFunction(()=>document.querySelector('.scan-page').dataset.cameraReady==='true');
    await page.locator('.pwa-mobile-nav a[href="/products"]').click();await page.locator('#pwa-search-query').waitFor();await page.waitForFunction(()=>scannerTracks.every(t=>t.readyState==='ended'));
    await page.locator('.pwa-mobile-nav a[href="/scan"]').click();await page.waitForFunction(()=>document.querySelector('.scan-page').dataset.cameraReady==='true');
    await page.evaluate(()=>window.testGtin='96385074');await page.getByRole('heading',{name:'We don’t have this one yet.',exact:true}).waitFor();await page.waitForFunction(()=>scannerTracks.every(t=>t.readyState==='ended'));
    result.cameraRequests=await page.evaluate(()=>cameraRequests);result.cameraReleased=true;
   }else{
    await page.locator('.scanner-state').filter({hasText:'Enable camera access'}).waitFor();result.permissionRecovery=true;await page.screenshot({path:out+'/production-permission-'+name+'.png'});
    await page.getByRole('button',{name:'Enter barcode number',exact:true}).click();await page.waitForFunction(()=>document.activeElement?.id==='gtin');await page.locator('#gtin').fill('96385074');await page.getByRole('button',{name:'Look up',exact:true}).click();await page.getByRole('heading',{name:'We don’t have this one yet.',exact:true}).waitFor();
   }
   check(await page.locator('.scan-page').getAttribute('data-capture')==='false','Capture stage exits');result.contributionPath=await page.getByRole('link',{name:'Add product',exact:true}).getAttribute('href');check(result.contributionPath==='/contribute/product?gtin=96385074','Existing unknown route');
   await page.getByRole('link',{name:'Add product',exact:true}).click();await page.getByRole('heading',{name:'Add a Missing Product',exact:true}).waitFor();check(await page.locator('.scanner-overlay').count()===0,'Contribution wizard is not camera layout');check(!await page.locator('#blazor-error-ui').isVisible(),'No circuit error');check(result.errors.length===0,'No browser errors');result.passed=true;
  }catch(e){result.failure=e.message;result.passed=false;}
  finally{result.ended=new Date().toISOString();report.sessions.push(result);console.log(JSON.stringify(result));await context.close();await browser.close();}
 }
 report.ended=new Date().toISOString();fs.writeFileSync(out+'/production-smoke.json',JSON.stringify(report,null,2)+'\n');if(report.sessions.some(s=>!s.passed))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1});

