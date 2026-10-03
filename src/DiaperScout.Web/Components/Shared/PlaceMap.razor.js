import { discoveryMarker, categoryLabel } from '/js/atlas-marker.js';
let library;
const maps = new WeakMap();
function loadLeaflet() {
    if (window.L) return Promise.resolve(window.L);
    library ??= new Promise((resolve, reject) => {
        const script=document.createElement('script');script.src='/lib/leaflet/leaflet.js';
        script.onload=()=>resolve(window.L);script.onerror=reject;document.head.append(script);
    }); return library;
}
export async function create(element, dotnet, picking, places, tileUrl, atlas = false) {
    const L=await loadLeaflet();if(!element.isConnected)return;
    const map=L.map(element,{scrollWheelZoom:false}).setView([52,-2],6);
    const entry={map,dotnet,active:true,atlas,markers:new Map()};maps.set(element,entry);
    if(atlas) {
        map.zoomControl.setPosition('topright');
        entry.discoveryLayer=L.layerGroup().addTo(map);
        map.on('moveend',()=>reportViewport(entry));
        const nav=document.querySelector('.pwa-mobile-nav');
        const measure=()=>{if(nav&&getComputedStyle(nav).display!=='none')element.closest('.atlas-page')?.style.setProperty('--atlas-nav-height',`${nav.getBoundingClientRect().height}px`);};
        entry.navObserver=new ResizeObserver(measure);if(nav)entry.navObserver.observe(nav);measure();
    }
    const tiles=L.tileLayer(tileUrl,{maxZoom:19,attribution:'© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>'});
    tiles.on('tileerror',()=>{if(entry.active&&!entry.failed){entry.failed=true;dotnet.invokeMethodAsync('MapUnavailable').catch(()=>{});}});tiles.addTo(map);
    const icon=L.divIcon({className:'place-map-pin',html:'<span aria-hidden="true">●</span>',iconSize:[40,40],iconAnchor:[20,20]});
    if(picking) {
        let selected;
        map.on('click',event=>{if(!entry.active)return;selected?.remove();selected=L.marker(event.latlng,{icon}).addTo(map);
            dotnet.invokeMethodAsync('SelectPosition',Number(event.latlng.lat.toFixed(6)),Number(event.latlng.lng.toFixed(6))).catch(()=>{});});
    } else if(atlas) {
        update(element,places,null);
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
    element.dataset.mapReady='true';
}
export function update(element, places, selectedId) {
    const entry=maps.get(element);if(!entry?.active||!entry.atlas)return;
    const L=window.L;
    // This dataset is strictly the public observation projection, not shop search.
    const discoveries=places.filter(item=>item.observations?.length>0&&Number.isFinite(item.place.latitude)&&Number.isFinite(item.place.longitude));
    const key=JSON.stringify(discoveries);
    if(entry.dataKey!==key) {
        entry.dataKey=key;entry.discoveryLayer.clearLayers();entry.markers.clear();
        const points=[];
        for(const item of discoveries) {
            const place=item.place,point=[place.latitude,place.longitude];points.push(point);
            const count=item.observations.length;
            const category=categoryLabel(place.category);
            const icon=L.divIcon({className:'atlas-discovery-marker',html:discoveryMarker(count,place.category),iconSize:[48,58],iconAnchor:[24,54],popupAnchor:[0,-48]});
            const marker=L.marker(point,{icon,title:place.name,alt:`${place.name}: ${count} observation${count===1?'':'s'}`,keyboard:true}).addTo(entry.discoveryLayer);
            const content=document.createElement('div'),name=document.createElement('strong'),summary=document.createElement('p');
            name.textContent=place.name;summary.textContent=`${count} observation${count===1?'':'s'} · ${item.productCount} pack${item.productCount===1?'':'s'} reported`;
            content.append(name,summary);marker.bindPopup(content);
            const select=()=>entry.dotnet.invokeMethodAsync('SelectPlace',place.id).catch(()=>{});
            marker.on('click',select);
            marker.on('keypress',event=>{
                if(event.originalEvent.key==='Enter'||event.originalEvent.key===' ') {
                    L.DomEvent.stop(event.originalEvent); marker.openPopup(); select();
                }
            });
            marker.getElement()?.setAttribute('aria-label',`${place.name}${category ? ` (${category})` : ''}: ${count} dated observation${count===1?'':'s'}. View discoveries.`);
            entry.markers.set(place.id,marker);
        }
        if(points.length&&!entry.fitted) {entry.fitted=true;if(points.length===1)entry.map.setView(points[0],15);else entry.map.fitBounds(points,{padding:[60,60],maxZoom:15});}
    }
    for(const [id,marker] of entry.markers)marker.getElement()?.classList.toggle('atlas-marker-selected',id===selectedId);
    if(selectedId&&entry.selection!==selectedId) {
        const marker=entry.markers.get(selectedId);
        if(marker) {
            const sheet=element.closest('.atlas-page')?.querySelector('.atlas-selected-place');
            const offset=sheet&&sheet.getBoundingClientRect().width>element.clientWidth*.8 ? sheet.getBoundingClientRect().height/2 : 0;
            const centre=entry.map.unproject(entry.map.project(marker.getLatLng()).add([0,offset]));
            entry.map.panTo(centre,{animate:!matchMedia('(prefers-reduced-motion: reduce)').matches});
        }
    }
    entry.selection=selectedId;
    reportViewport(entry);
}
function reportViewport(entry) {
    if(!entry.active)return;
    const count=[...entry.markers.values()].filter(marker=>entry.map.getBounds().contains(marker.getLatLng())).length;
    if(count!==entry.visibleCount) {entry.visibleCount=count;entry.dotnet.invokeMethodAsync('ViewportChanged',count).catch(()=>{});}
}
export function destroy(element) {const entry=maps.get(element);if(!entry)return;entry.active=false;entry.observer?.disconnect();entry.navObserver?.disconnect();entry.map.remove();delete element.dataset.mapReady;maps.delete(element);}
