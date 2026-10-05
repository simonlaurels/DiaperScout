async function request(root, path, body) {
    const csrf=root.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const response=await fetch(path,{method:body===undefined?'GET':'POST',credentials:'same-origin',cache:'no-store',
        headers:{'Content-Type':'application/json','X-CSRF-TOKEN':csrf??''},body:body===undefined?undefined:JSON.stringify(body)});
    if(response.redirected) { const error=Error('Verify your email to continue.');error.restart=true;throw error; }
    const result=await response.json().catch(()=>null);
    if(!response.ok) { const error=Error(result?.message??(response.status===429?'Too many attempts. Please wait a few minutes and try again.':'DiaperScout couldn’t connect just now. Please try again.'));error.restart=result?.restart;throw error; }
    return result;
}
const message=root=>root.querySelector('[data-onboarding-message], [data-passkey-message]');
function busy(root, button, text) { root.dataset.busy='true';root.setAttribute('aria-busy','true');const original=button.textContent;button.textContent=text;root.querySelectorAll('button').forEach(b=>b.disabled=true);return()=>{delete root.dataset.busy;root.removeAttribute('aria-busy');button.textContent=original;root.querySelectorAll('button').forEach(b=>b.disabled=false);}; }
async function finish(root, skip, button) {
    if(root.dataset.busy)return;
    const reset=busy(root,button,'Saving…');message(root).textContent='';
    try { await request(root,'/join/data/complete',{skip});location.assign('/join/ready'); }
    catch {message(root).textContent='Your progress is safe. We couldn’t finish just now. Please try again.';reset();}
}
async function state(root) {
    const loading=root.querySelector('[data-onboarding-loading]');const retry=root.querySelector('[data-onboarding-retry]');
    retry.hidden=true;loading.hidden=false;loading.textContent='Opening your Explorer ID…';
    try {
        const data=await request(root,'/join/data/state');loading.hidden=true;
        if(!data.email)throw Error('Verify your email before continuing.');
        root.querySelector('[data-onboarding-name-needed]').hidden=!!data.displayName;
        const complete=!!data.displayName && ['passkey','skipped'].includes(data.stage);
        if(complete)message(root).textContent='';
        root.querySelector('[data-onboarding-success]').hidden=!complete;
        root.querySelector('[data-onboarding-setup]').hidden=!data.displayName || complete;
        root.querySelector('[data-onboarding-name]').textContent=data.displayName??'';
        const key=complete && data.stage==='passkey' && data.hasPasskey;
        root.querySelector('[data-onboarding-key]').hidden=!key;
        root.querySelector('[data-onboarding-key-name]').textContent=data.displayName??'';
        root.querySelector('[data-onboarding-success-title]').textContent=key?"You’ve got your key!":"Your Explorer ID is ready!";
        root.querySelector('[data-onboarding-success-copy]').textContent=key?'Your email is verified and your passkey is ready to use.':'Your email is verified. You can sign in with a secure email link.';
        root.querySelector('[data-onboarding-reminder]').hidden=key;
        root.querySelector('[data-onboarding-existing]').hidden=!data.hasPasskey;
        root.querySelector('[data-onboarding-reverify]').hidden=data.recent;
        root.querySelector('[data-passkey-action="register"]').disabled=!data.recent;
    } catch(error) {
        loading.hidden=false;loading.textContent=error.message;
        retry.hidden=false;
        if(error.restart) { retry.textContent='Verify your email';retry.dataset.restart='true'; }
    }
}
document.addEventListener('submit',async event=>{
    const form=event.target.closest?.('[data-onboarding-form], [data-onboarding-name-form]');if(!form)return;
    event.preventDefault();const root=form.closest('[data-onboarding-public], [data-onboarding-private]');if(root.dataset.busy)return;
    const name=form.querySelector('[name="displayName"]');name.value=name.value.trim();name.setCustomValidity(name.value?'':'Enter your name or nickname.');
    if(!form.reportValidity())return;
    const reset=busy(root,form.querySelector('button[type="submit"]'),form.hasAttribute('data-onboarding-form')?'Sending link…':'Saving…');message(root).textContent='';
    try {
        if(form.hasAttribute('data-onboarding-form')) { await request(root,'/join/data/request',{displayName:name.value,email:form.querySelector('[name="email"]').value});location.assign('/join/check-email'); }
        else {
            const response=await fetch('/backpack/data/name',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':root.querySelector('input[name="__RequestVerificationToken"]').value},body:JSON.stringify({displayName:name.value})});
            if(!response.ok) { const result=await response.json().catch(()=>null);throw Error(result?.errors?.displayName?.[0]??'We couldn’t save your name. Please try again.'); }
            reset();await state(root);
        }
    } catch(error) {message(root).textContent=error.message;reset();}
});
document.addEventListener('input',event=>{if(event.target.matches?.('[data-onboarding-form] [name="displayName"], [data-onboarding-name-form] [name="displayName"]'))event.target.setCustomValidity('');});
document.addEventListener('click',async event=>{
    const root=event.target.closest?.('[data-onboarding-public], [data-onboarding-private]');if(!root)return;
    const button=event.target.closest('button');if(!button)return;
    if(button.hasAttribute('data-onboarding-skip'))return finish(root,true,button);
    if(button.hasAttribute('data-onboarding-existing'))return finish(root,false,button);
    if(button.hasAttribute('data-onboarding-retry')) { if(button.dataset.restart)location.assign('/join/details');else await state(root);return; }
    if(button.hasAttribute('data-onboarding-resend')&&!root.dataset.busy) {
        const reset=busy(root,button,'Sending link…');
        try { await request(root,'/join/data/resend',{});message(root).textContent='Check your inbox. Please allow a minute between resend requests.'; }
        catch(error) {message(root).textContent=error.message;}finally{reset();}
    }
});
async function initialise() {
    const root=document.querySelector('[data-onboarding-private]');if(root)await state(root);
    const waiting=document.querySelector('[data-onboarding-email]');if(waiting){
        try { const pending=await request(waiting.closest('[data-onboarding-public]'),'/join/data/pending');waiting.textContent=pending.email; }
        catch {const root=waiting.closest('[data-onboarding-public]');message(root).textContent='Open your email link to continue, or enter your details again to request a fresh link.';root.querySelector('[data-onboarding-resend]').disabled=true;}
    }
}
initialise();
if(window.Blazor?.addEventListener)Blazor.addEventListener('enhancedload',initialise);
else addEventListener('diaperscout:framework-ready',()=>Blazor.addEventListener('enhancedload',initialise),{once:true});
