const standalone = () => matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
document.addEventListener('click',event=>{
    if(event.target.closest?.('.backpack-page a[href="#backpack-add-passkey"]')) {
        event.preventDefault();
        const panel=document.getElementById('backpack-add-passkey');
        if(panel){panel.open=true;panel.scrollIntoView({block:'start'});}
    }
});
export function renderBackpackPasskeys(list, passkeys) {
    if(list.closest('[data-backpack-home]')){
        list.setAttribute('aria-label',`${passkeys.length} registered passkeys`);
        for(const passkey of passkeys){
            const link=document.createElement('a');link.className='backpack-key backpack-hanging-key';link.href='/backpack/passkeys';
            const name=passkey.name?.trim() || 'Passkey';link.setAttribute('aria-label',`Manage passkey ${name}`);
            link.title=`Added ${new Date(passkey.createdAtUtc).toLocaleDateString()}`;
            const icon=document.createElement('span');icon.className='backpack-physical-key';icon.setAttribute('aria-hidden','true');
            icon.innerHTML='<svg viewBox="0 0 80 140" fill="none" stroke="currentColor" stroke-width="9" stroke-linejoin="round"><circle cx="40" cy="30" r="22"/><circle cx="40" cy="30" r="5"/><path d="M40 52v72h23v-16H48V91h13"/></svg>';
            const tag=document.createElement('span');tag.className='backpack-key-tag';const label=document.createElement('strong');label.textContent=name;
            const copy=document.createElement('span');copy.textContent='Passkey';tag.append(label,copy);link.append(icon,tag);list.append(link);
        }
        if(!passkeys.length){const copy=list.closest('[data-backpack-home]').querySelector('[data-backpack-passkey-copy]');copy.textContent='There’s room for a key. Add a passkey when you’re ready.';}
        return;
    }
    if (!passkeys.length) {
        const heading=document.createElement('h3');heading.textContent='There’s room for a key…';
        const copy=document.createElement('p');copy.textContent='Add a passkey for a quicker sign-in. Your email sign-in links are ready to use either way.';
        const link=document.createElement('a');link.href='#backpack-add-passkey';link.dataset.enhanceNav='false';link.textContent='Add a passkey';
        const empty=document.createElement('div');empty.className='backpack-no-keys';empty.append(heading,copy,link);list.append(empty);return;
    }
    for(const passkey of passkeys){
        const row=document.createElement('details');row.className='backpack-key';
        const summary=document.createElement('summary');
        const icon=document.createElement('span');icon.className='backpack-physical-key';icon.setAttribute('aria-hidden','true');
        icon.innerHTML='<svg viewBox="0 0 80 140" fill="none" stroke="currentColor" stroke-width="9" stroke-linejoin="round"><circle cx="40" cy="30" r="22"/><circle cx="40" cy="30" r="5"/><path d="M40 52v72h23v-16H48V91h13"/></svg>';
        const tag=document.createElement('span');tag.className='backpack-key-tag';
        const name=document.createElement('strong');name.textContent=passkey.name?.trim() || 'Passkey';
        const label=document.createElement('span');label.textContent='Passkey · Open tag';tag.append(name,label);summary.append(icon,tag);
        const dates=document.createElement('p');dates.textContent=`Added ${new Date(passkey.createdAtUtc).toLocaleDateString()} · ${passkey.lastUsedAtUtc ? 'Last used '+new Date(passkey.lastUsedAtUtc).toLocaleDateString() : 'Not used yet'}`;
        const button=document.createElement('button');button.type='button';button.dataset.passkeyAction='remove';button.dataset.passkeyId=passkey.id;button.textContent='Remove passkey';button.setAttribute('aria-label',`Remove passkey ${name.textContent}`);
        row.append(summary,dates,button);list.append(row);
    }
}
async function initialise(){
    if(!standalone())return;
    const root=document.querySelector('[data-backpack-account]');if(!root)return;
    if(root.dataset.identityLoaded && root.querySelector('[data-backpack-name]').textContent !== 'Your Explorer identity')return;
    root.dataset.identityLoaded='true';const status=root.querySelector('[data-backpack-identity-status]');
    try{const response=await fetch('/backpack/identity',{credentials:'same-origin',cache:'no-store'});if(!response.ok || response.redirected)throw Error();const identity=await response.json();root.querySelector('[data-backpack-name]').textContent=identity.displayName || 'Explorer';status.textContent=identity.displayName ? '' : 'Your sign-in account is ready. No Explorer name is on record.';}
    catch{status.textContent='Your ID could not be opened. Refresh to try again. Passkey management and sign-out remain available.';delete root.dataset.identityLoaded;}
}
initialise();
if(window.Blazor?.addEventListener)Blazor.addEventListener('enhancedload',initialise);
else addEventListener('diaperscout:framework-ready',()=>Blazor.addEventListener('enhancedload',initialise),{once:true});
