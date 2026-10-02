// Capture an intentionally incomplete document: fonts.ready can await parser completion in WebKit.
// The bundled screenshotter supports skipping that capture-only wait; application fonts are untouched.
process.env.PW_TEST_SCREENSHOT_NO_FONTS_READY = "1";
const fs=require('fs'),path=require('path');const {chromium,webkit,devices}=require(path.resolve('tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/.playwright/package'));
(async()=>{const checks=[];for(const [name,engine] of [['chromium',chromium],['webkit',webkit]]){
 const browser=await engine.launch();const context=await browser.newContext({...devices['iPhone 13'],colorScheme:'dark',serviceWorkers:'block'});await context.addInitScript(()=>Object.defineProperty(navigator,'standalone',{value:true}));
 const page=await context.newPage();let release;const gate=new Promise(r=>release=r);
 await page.route('**/_framework/blazor*js',async route=>{await gate;await route.abort();});
 try{await page.goto('https://diaperscout.app/',{waitUntil:'commit'});await page.locator('#pwa-startup-title').waitFor();
  await page.waitForFunction(()=>document.querySelector('.pwa-guide')?.complete&&document.querySelector('.pwa-guide')?.naturalWidth>0&&document.querySelector('.pwa-startup-brand')?.complete);
  const state=await page.evaluate(()=>({background:getComputedStyle(document.documentElement).backgroundColor,colorScheme:getComputedStyle(document.documentElement).colorScheme,title:document.querySelector('#pwa-startup-title').textContent,visible:getComputedStyle(document.querySelector('#pwa-startup')).display,frameworkLoaded:typeof Blazor!=='undefined',appLoaded:typeof DiaperScoutPwa!=='undefined'}));
  if(state.background!=='rgb(250, 245, 236)'||state.visible!=='flex'||state.frameworkLoaded||state.appLoaded)throw new Error('Pre-framework branded startup absent: '+JSON.stringify(state));
  await page.screenshot({path:`docs/implementation/pwa-reliability-evidence/production-startup-${name}.png`});checks.push({engine:name,simulatedStandalone:true,darkPreference:true,passed:true,state});
 }catch(e){checks.push({engine:name,passed:false,error:e.message});process.exitCode=1;}finally{release();await context.close();await browser.close();}
 }fs.writeFileSync('docs/implementation/pwa-reliability-evidence/production-static-startup.json',JSON.stringify(checks,null,2)+'\n');console.log(JSON.stringify(checks));})();
