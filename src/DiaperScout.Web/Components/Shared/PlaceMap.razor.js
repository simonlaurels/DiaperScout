let library;
const maps = new WeakMap();
function loadLeaflet() {
    if (window.L) return Promise.resolve(window.L);
    library ??= new Promise((resolve, reject) => {
        const script=document.createElement('script');script.src='/lib/leaflet/leaflet.js';
        script.onload=()=>resolve(window.L);script.onerror=reject;document.head.append(script);
    }); return library;
}
export async function create(element, dotnet, picking, places, tileUrl) {
    const L=await loadLeaflet();if(!element.isConnected)return;
    const map=L.map(element,{scrollWheelZoom:false}).setView([52,-2],6);
    const entry={map,dotnet,active:true};maps.set(element,entry);
    const tiles=L.tileLayer(tileUrl,{maxZoom:19,attribution:'© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>'});
    tiles.on('tileerror',()=>{if(entry.active&&!entry.failed){entry.failed=true;dotnet.invokeMethodAsync('MapUnavailable').catch(()=>{});}});tiles.addTo(map);
    const icon=L.divIcon({className:'place-map-pin',html:'<span aria-hidden="true">●</span>',iconSize:[40,40],iconAnchor:[20,20]});
    if(picking) {
        let selected;
        map.on('click',event=>{if(!entry.active)return;selected?.remove();selected=L.marker(event.latlng,{icon}).addTo(map);
            dotnet.invokeMethodAsync('SelectPosition',Number(event.latlng.lat.toFixed(6)),Number(event.latlng.lng.toFixed(6))).catch(()=>{});});
    } else {
        const points=[];
        for(const item of places) {
            const place=item.place;const point=[place.latitude,place.longitude];points.push(point);
            const marker=L.marker(point,{icon,title:place.name,alt:place.name}).addTo(map);
            const content=document.createElement('div'),name=document.createElement('strong'),summary=document.createElement('p'),button=document.createElement('button');
            name.textContent=place.name;summary.textContent=`${item.productCount} pack${item.productCount===1?'':'s'} reported · last observed ${new Date(item.latestObservedAtUtc).toLocaleDateString()}`;
            button.type='button';button.textContent='View observations';button.addEventListener('click',()=>dotnet.invokeMethodAsync('SelectPlace',place.id).catch(()=>{}));
            content.append(name,summary,button);marker.bindPopup(content);marker.on('click',()=>dotnet.invokeMethodAsync('SelectPlace',place.id).catch(()=>{}));
        }
        if(points.length===1)map.setView(points[0],15);else if(points.length>1)map.fitBounds(points,{padding:[35,35],maxZoom:15});
    }
    const observer=new ResizeObserver(()=>{if(entry.active)map.invalidateSize();});observer.observe(element);entry.observer=observer;
}
export function destroy(element) {const entry=maps.get(element);if(!entry)return;entry.active=false;entry.observer.disconnect();entry.map.remove();maps.delete(element);}
