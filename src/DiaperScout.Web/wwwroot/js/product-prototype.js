// The approved prototype gallery is local UI state, independent of server circuit latency.
const galleries = new WeakMap();

export function gallery(element, initialIndex) {
    if (!element?.isConnected) return false;
    galleries.get(element)?.();
    const track = element.querySelector('.gallery-track');
    const counter = element.querySelector('.gallery-counter');
    if (!track || !counter) return false;
    const count = track.children.length;
    let index = initialIndex;
    let start = null;
    const move = direction => {
        index = (index + direction + count) % count;
        track.style.transform = `translateX(-${index * 100}%)`;
        counter.textContent = `${index + 1} / ${count}`;
    };
    const down = event => {
        if (!event.isPrimary) return;
        start = event.clientX;
        element.setPointerCapture(event.pointerId);
    };
    const up = event => {
        if (start === null || !event.isPrimary) return;
        const distance = event.clientX - start;
        start = null;
        if (Math.abs(distance) > 40) move(distance < 0 ? 1 : -1);
    };
    const cancel = () => { start = null; };
    const keyboard = event => {
        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
        event.preventDefault();
        move(event.key === 'ArrowRight' ? 1 : -1);
    };
    element.addEventListener('pointerdown', down);
    element.addEventListener('pointerup', up);
    element.addEventListener('pointercancel', cancel);
    element.addEventListener('keydown', keyboard);
    element.dataset.galleryReady = 'true';
    // Navigation removes interactive roots before server-side component disposal.
    // Clean up locally rather than issuing interop against a discarded gallery.
    const observer = new MutationObserver(() => {
        if (!element.isConnected) dispose();
    });
    const dispose = () => {
        element.removeEventListener('pointerdown', down);
        element.removeEventListener('pointerup', up);
        element.removeEventListener('pointercancel', cancel);
        element.removeEventListener('keydown', keyboard);
        delete element.dataset.galleryReady;
        observer.disconnect();
        galleries.delete(element);
    };
    galleries.set(element, dispose);
    observer.observe(document.body, { childList: true, subtree: true });
    return true;
}
export async function share(title,url) {if(navigator.share){try{await navigator.share({title,url});return 'Shared';}catch(e){if(e.name==='AbortError')return '';throw e;}}await navigator.clipboard.writeText(url);return 'Link copied';}
export async function locate() {return new Promise((resolve,reject)=>{if(!navigator.geolocation)return reject(new Error('Location unavailable'));navigator.geolocation.getCurrentPosition(p=>resolve({latitude:p.coords.latitude,longitude:p.coords.longitude}),reject,{timeout:10000,maximumAge:60000,enableHighAccuracy:false});});}
