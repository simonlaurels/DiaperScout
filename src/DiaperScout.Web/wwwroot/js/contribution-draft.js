const prefix='diaperscout-product-draft:';
export function localNow() {const date=new Date();date.setMinutes(date.getMinutes()-date.getTimezoneOffset());return date.toISOString().slice(0,16);}
export function toUtc(value) {const date=new Date(value);return Number.isNaN(date.getTime())?'':date.toISOString();}
export function saveDraft(gtin, draft) {try {sessionStorage.setItem(prefix+gtin,JSON.stringify({expires:Date.now()+30*60*1000,draft}));return true;}catch{return false;}}
export function loadDraft(gtin) {try {const item=JSON.parse(sessionStorage.getItem(prefix+gtin)||'null');if(!item||item.expires<Date.now()){clearDraft(gtin);return null;}return item.draft;}catch{return null;}}
export function clearDraft(gtin) {try{sessionStorage.removeItem(prefix+gtin);}catch{}}
