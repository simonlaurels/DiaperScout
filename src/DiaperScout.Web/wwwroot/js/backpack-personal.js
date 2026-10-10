import {listOwnedDrafts} from './contribution-draft.js';

async function read(path){
    const response=await fetch(path,{credentials:'same-origin',cache:'no-store'});
    if(!response.ok || response.redirected)throw Error('We couldn’t open your account information. Refresh or sign in again.');
    return response.json();
}
async function drafts(){
    const [context,server]=await Promise.all([read('/backpack/data/context'),read('/backpack/data/drafts')]);
    let local=[],storageUnavailable=false;
    try{local=await listOwnedDrafts(context.ownerTag);}catch{storageUnavailable=true;}
    return {items:[...local.map(d=>({...d,url:`/contribute/product?gtin=${encodeURIComponent(d.gtin)}`})),...server.items],total:local.length+server.totalCount,storageUnavailable};
}
function card(name,copy,url){
    const row=document.createElement('article');row.className='backpack-personal-item';
    const heading=document.createElement('h2');heading.textContent=name;
    const description=document.createElement('p');description.textContent=copy;row.append(heading,description);
    if(url){const link=document.createElement('a');link.href=url;link.textContent='Open ›';row.append(link);}
    return row;
}
async function initialise(){
    const summary=document.querySelector('[data-backpack-draft-summary]');
    if(summary && (matchMedia('(display-mode: standalone)').matches || navigator.standalone===true)){try{const data=await drafts();summary.textContent=data.storageUnavailable?'Open drafts. Device storage is unavailable.':data.total===1?'You have 1 draft waiting to be finished.':data.total?`You have ${data.total} drafts waiting to be finished.`:'No unfinished drafts.';}catch{summary.textContent='Open your unfinished drafts.';}}
    const journey=document.querySelector('[data-backpack-journey]');
    if(journey){const content=journey.querySelector('[data-personal-content]');try{const data=await drafts();content.replaceChildren();if(!data.items.length)content.textContent='Your journey is up to date. No unfinished drafts.';for(const item of data.items)content.append(card(item.name,item.expires?`${item.brand} · Recovery copy expires ${new Date(item.expires).toLocaleTimeString()}`:`Catalogue draft · Updated ${new Date(item.updatedAtUtc).toLocaleDateString()}`,item.url));if(data.storageUnavailable){const note=document.createElement('p');note.textContent='Device draft storage is unavailable. Your catalogue drafts are still shown.';content.append(note);}if(data.items.length<data.total){const note=document.createElement('p');note.textContent='Showing the latest 50 catalogue drafts and recovery copies on this device.';content.append(note);}}catch(error){content.textContent='Your drafts could not be opened. No drafts were deleted. '+error.message;}}
    const discoveries=document.querySelector('[data-backpack-discoveries]');
    if(discoveries){const content=discoveries.querySelector('[data-personal-content]');try{const items=await read('/backpack/data/discoveries');content.replaceChildren();if(!items.length)content.textContent='Your discoveries start here. Scan a product and record a find.';for(const item of items)content.append(card(item.name,`${item.kind} · ${item.status} · ${new Date(item.occurredAtUtc).toLocaleDateString()}`,item.url));}catch(error){content.textContent=error.message;}}
    const settings=document.querySelector('[data-backpack-settings]');
    if(settings){
        const preference=settings.querySelector('[data-keep-product-drafts]');
        try{preference.checked=localStorage.getItem('ds-keep-product-drafts-v1')!=='no';}catch{preference.disabled=true;settings.querySelector('[data-preference-message]').textContent='Browser storage is unavailable.';}
        const message=settings.querySelector('[data-profile-message]');
        try{const account=await read('/backpack/data/account');settings.querySelector('[data-account-email]').textContent=account.email || 'No email is on record.';
            const input=settings.querySelector('#explorer-name');input.value=account.displayName || '';input.disabled=!account.displayName;settings.querySelector('[data-save-explorer-name]').disabled=!account.displayName;
            message.textContent=account.displayName?'':'This account has no Explorer profile to edit.';
        }catch(error){message.textContent=error.message;settings.querySelector('[data-account-email]').textContent='Unavailable.';}
    }
}
document.addEventListener('change',event=>{
    if(!event.target.matches?.('[data-keep-product-drafts]'))return;
    const message=document.querySelector('[data-preference-message]');
    try{localStorage.setItem('ds-keep-product-drafts-v1',event.target.checked?'yes':'no');message.textContent='Preference saved on this device.';}catch{message.textContent='This browser could not save the preference.';}
});
document.addEventListener('click',async event=>{
    const button=event.target.closest?.('[data-save-explorer-name]');if(!button)return;
    const root=button.closest('[data-backpack-settings]'),input=root.querySelector('#explorer-name'),message=root.querySelector('[data-profile-message]');
    if(!input.value.trim() || input.value.trim().length>100){message.textContent='Enter an Explorer name of 1 to 100 characters.';return;}
    button.disabled=true;
    try{
        const response=await fetch('/backpack/data/name',{method:'POST',credentials:'same-origin',cache:'no-store',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':root.querySelector('input[name="__RequestVerificationToken"]').value},body:JSON.stringify({displayName:input.value})});
        if(!response.ok){if(response.status===400){const problem=await response.json();throw Error(Object.values(problem.errors||{}).flat().join(' ') || 'Check the name.');}throw Error('Your name could not be saved. Refresh or sign in again.');}
        input.value=input.value.trim();message.textContent='Explorer name saved.';
    }catch(error){message.textContent=error.message;}finally{button.disabled=false;}
});
initialise();
if(window.Blazor?.addEventListener)Blazor.addEventListener('enhancedload',initialise);
else addEventListener('diaperscout:framework-ready',()=>Blazor.addEventListener('enhancedload',initialise),{once:true});
