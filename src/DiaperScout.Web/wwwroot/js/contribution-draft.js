const prefix='diaperscout-product-draft:';
export function isInstalled() { return navigator.standalone === true || matchMedia('(display-mode: standalone)').matches; }
let locationDenied = false;
export async function nearbyPosition() {
    if (locationDenied) return {status:'denied'};
    if (!navigator.geolocation) return {status:'unavailable'};
    return await new Promise(resolve => navigator.geolocation.getCurrentPosition(p => resolve({status:'found',latitude:p.coords.latitude,longitude:p.coords.longitude}), e => {
        if (e.code === 1) locationDenied = true;
        resolve({status:e.code === 1 ? 'denied' : 'unavailable'});
    }, {timeout:12000,maximumAge:60000,enableHighAccuracy:false}));
}
export function localNow() {const date=new Date();date.setMinutes(date.getMinutes()-date.getTimezoneOffset());return date.toISOString().slice(0,16);}
export function toUtc(value) {const date=new Date(value);return Number.isNaN(date.getTime())?'':date.toISOString();}
// No authentication material is stored. Keep the stable contribution ID for retry deduplication.
export function saveDraft(gtin, draft, owner=null, step=3) {
    try { if(localStorage.getItem('ds-keep-product-drafts-v1')==='no')return false; } catch {}
    try {localStorage.setItem(prefix+gtin,JSON.stringify({expires:Date.now()+30*60*1000,draft,owner,step}));return true;}
    catch {return false;}
}
// Enumerate the same recovery copies without extending expiry or exposing another owner's draft.
export async function listOwnedDrafts(ownerTag) {
    const keys=new Set();
    for(const storage of [localStorage,sessionStorage]) {
        for(let i=0;i<storage.length;i++){const key=storage.key(i);if(key?.startsWith(prefix))keys.add(key);}
    }
    const results=[];
    for(const key of keys) {
        try {
            const item=JSON.parse(localStorage.getItem(key)||sessionStorage.getItem(key)||'null');
            const gtin=key.slice(prefix.length);
            if(!item || !Number.isFinite(item.expires) || item.expires<=Date.now() || !/^(?:\d{8}|\d{12,14})$/.test(gtin) || !item.draft?.contributionId || item.draft?.submitted)continue;
            if(item.owner){
                const hash=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(item.owner));
                const tag=Array.from(new Uint8Array(hash),n=>n.toString(16).padStart(2,'0')).join('').toUpperCase();
                if(tag!==ownerTag)continue;
            }
            results.push({gtin,name:item.draft.productName || 'Unfinished product',brand:item.draft.brandName || '',expires:item.expires});
        } catch { /* A damaged entry must not hide the other recovery copies. */ }
    }
    return results.sort((a,b)=>b.expires-a.expires);
}
export function loadDraft(gtin, owner=null) {
    try {
        let item=JSON.parse(localStorage.getItem(prefix+gtin)||'null');
        // Preserve drafts written by the preceding app version in this same tab.
        if(!item) item=JSON.parse(sessionStorage.getItem(prefix+gtin)||'null');
        if(!item) return null;
        if(!Number.isFinite(item.expires) || item.expires<Date.now()) {clearDraft(gtin);return null;}
        if(item.owner && item.owner!==owner) return null;
        return item.draft;
    } catch {return null;}
}
export function draftStep(gtin) {
    try {const item=JSON.parse(localStorage.getItem(prefix+gtin)||sessionStorage.getItem(prefix+gtin)||'null');return [1,2,3].includes(item?.step)?item.step:3;}catch{return 3;}
}
export function clearDraft(gtin, contributionId=null) {
    if(contributionId) {
        try {const item=JSON.parse(localStorage.getItem(prefix+gtin)||sessionStorage.getItem(prefix+gtin)||'null');
            if(item?.draft?.contributionId!==contributionId) return;
        }catch{return;}
    }
    try{localStorage.removeItem(prefix+gtin);}catch{}
    try{sessionStorage.removeItem(prefix+gtin);}catch{}
}
