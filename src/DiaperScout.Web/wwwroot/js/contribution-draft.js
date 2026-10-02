const prefix='diaperscout-product-draft:';
export function localNow() {const date=new Date();date.setMinutes(date.getMinutes()-date.getTimezoneOffset());return date.toISOString().slice(0,16);}
export function toUtc(value) {const date=new Date(value);return Number.isNaN(date.getTime())?'':date.toISOString();}
// No authentication material is stored. Keep the stable contribution ID for retry deduplication.
export function saveDraft(gtin, draft, owner=null, step=3) {
    try {localStorage.setItem(prefix+gtin,JSON.stringify({expires:Date.now()+30*60*1000,draft,owner,step}));return true;}
    catch {return false;}
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
