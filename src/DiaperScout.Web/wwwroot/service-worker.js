// Only this sealed, public offline shell is cached. Bump the version when any listed asset changes.
const CACHE = 'diaperscout-pwa-static-v2';
const STATIC = new Set([
  '/pwa/offline.html', '/pwa/offline.css', '/pwa/offline.js',
  '/images/brand/DiaperScout.svg', '/pwa/guide-map.webp',
  '/pwa/icons/icon-192.png', '/pwa/icons/icon-512.png', '/pwa/icons/maskable-512.png', '/pwa/icons/apple-touch-icon.png'
]);
self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE).then(cache => cache.addAll([...STATIC])));
  // A waiting update activates only after an explicit operator/user action or all old tabs close.
});
self.addEventListener('activate', event => {
  event.waitUntil((async () => {
    for (const key of await caches.keys())
      if (key.startsWith('diaperscout-pwa-static-') && key !== CACHE) await caches.delete(key);
    await self.clients.claim();
  })());
});
self.addEventListener('message', event => {
  if (event.data?.type === 'ACTIVATE_UPDATE') self.skipWaiting();
});
self.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== self.location.origin) return;
  if (request.mode === 'navigate') {
    event.respondWith((async () => {
      // Never retain catalogue, auth, admin, affiliate or other application documents.
      // A cold server gets a bounded wait; the offline page also describes an unreachable server.
      const controller = new AbortController();
      const deadline = setTimeout(() => controller.abort(), 20000);
      try {
        const response = await fetch(request, {cache: 'no-store', signal: controller.signal});
        if (response.status >= 500) return await caches.match('/pwa/offline.html') || response;
        return response;
      }
      catch { return await caches.match('/pwa/offline.html') || Response.error(); }
      finally { clearTimeout(deadline); }
    })());
    return;
  }
  // Exact pathname allowlist, no query strings: API, auth, _blazor, uploads and third-party requests pass straight through.
  if (!STATIC.has(url.pathname) || url.search) return;
  event.respondWith(caches.open(CACHE).then(async cache => (await cache.match(request)) || fetch(request)));
});
