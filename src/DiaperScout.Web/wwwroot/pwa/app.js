// App chrome is independent of server-side component connectivity. No credentials or catalogue data are stored here.
(() => {
    let ready = false, requestedUpdate = false, installEvent = null, waitingWorker = null, dismissedUpdate = null;
    const html = document.documentElement;
    const standalone = () => matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
    html.dataset.standalone = String(standalone());
    html.dataset.pwaState = 'starting';
    const slow = setTimeout(() => {
        if (ready) return;
        html.dataset.pwaState = 'slow';
        document.getElementById('pwa-startup-description').textContent = 'The map is taking a little longer to open. Thanks for waiting.';
    }, 8000);
    const failed = setTimeout(() => failure(), 45000);
    function failure() {
        if (ready) return;
        clearTimeout(slow); clearTimeout(failed);
        html.dataset.pwaState = 'failed';
        document.getElementById('pwa-startup-title').textContent = 'We couldn’t connect.';
        document.getElementById('pwa-startup-description').textContent = navigator.onLine
            ? 'The connection to DiaperScout didn’t start. Reload to try again.'
            : 'You’re offline. Reconnect, then try again.';
        document.getElementById('pwa-startup-retry').hidden = false;
    }
    window.DiaperScoutPwa = {
        markInteractiveReady() {
            ready = true; clearTimeout(slow); clearTimeout(failed);
            html.dataset.pwaState = 'ready';
            syncChrome();
            dispatchEvent(new Event('diaperscout:ready'));
        }
    };
    function getPreference(key) { try { return localStorage.getItem(key); } catch { return null; } }
    function setPreference(key, value) { try { localStorage.setItem(key, value); } catch {} }
    function syncChrome() {
        // Enhanced navigation patches document attributes as well as view content.
        html.dataset.standalone = String(standalone());
        if (ready) html.dataset.pwaState = 'ready';
        for (const button of document.querySelectorAll('[data-pwa-install]'))
            button.hidden = standalone() || !installEvent || getPreference('ds-install-dismissed') === 'true';
        for (const hint of document.querySelectorAll('[data-pwa-ios]'))
            hint.hidden = standalone() || !(/iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1));
        for (const link of document.querySelectorAll('.pwa-mobile-nav a')) {
            const active = link.pathname === '/' ? location.pathname === '/' : location.pathname.startsWith(link.pathname);
            if (active) link.setAttribute('aria-current', 'page'); else link.removeAttribute('aria-current');
        }
        for (const details of document.querySelectorAll('.ds-filter-disclosure:not([data-sized])')) {
            details.open = !matchMedia('(max-width: 900px)').matches;
            details.dataset.sized = 'true';
        }
        const update = document.getElementById('pwa-update');
        if (update) update.hidden = !waitingWorker || waitingWorker === dismissedUpdate;
    }
    addEventListener('beforeinstallprompt', event => {
        event.preventDefault(); installEvent = event; syncChrome();
        // Offer an explicit button only. No unsolicited prompt or repeating banner.
    });
    addEventListener('appinstalled', () => { installEvent = null; html.dataset.standalone = String(standalone()); syncChrome(); });
    document.addEventListener('click', async event => {
        if (!(event.target instanceof Element)) return;
        if (event.target.closest('#pwa-startup-retry')) { location.reload(); return; }
        if (event.target.closest('[data-pwa-install]') && installEvent) {
            const deferred = installEvent; installEvent = null;
            await deferred.prompt();
            const result = await deferred.userChoice;
            if (result.outcome === 'dismissed') setPreference('ds-install-dismissed', 'true');
            syncChrome();
        }
        if (event.target.closest('[data-pwa-update]') && waitingWorker) {
            if (!confirm('Reload to update DiaperScout? Finish or save anything you’re working on first.')) return;
            requestedUpdate = true;
            waitingWorker.postMessage({type: 'ACTIVATE_UPDATE'});
        }
        if (event.target.closest('[data-pwa-update-dismiss]')) { dismissedUpdate = waitingWorker; syncChrome(); }
    });
    const connection = () => {
        const notice = document.getElementById('pwa-offline-notice');
        if (notice) notice.hidden = navigator.onLine;
    };
    addEventListener('online', connection); addEventListener('offline', connection);
    connection(); syncChrome();
    const observer = new MutationObserver(() => syncChrome());
    // Only listen for added views; avoid observing attributes we ourselves change.
    observer.observe(document.querySelector('.ds-main'), {childList: true, subtree: true});
    if ('serviceWorker' in navigator && isSecureContext) {
        navigator.serviceWorker.register('/service-worker.js', {scope: '/', updateViaCache: 'none'}).then(registration => {
            const showWaiting = () => { if (registration.waiting && navigator.serviceWorker.controller) { waitingWorker = registration.waiting; syncChrome(); } };
            showWaiting();
            registration.addEventListener('updatefound', () => {
                registration.installing?.addEventListener('statechange', showWaiting);
            });
        }).catch(() => { /* The connected application remains fully usable without offline support. */ });
        navigator.serviceWorker.addEventListener('controllerchange', () => { if (requestedUpdate) location.reload(); });
    }
    // Keep startup static: it must not create a circuit on pages that otherwise use SSR.
    if (!window.Blazor) { failure(); return; }
    function startFramework() {
        // Server-rendered component state is emitted after the body scripts. Wait for the
        // complete document before reading it, including on reload/history navigation.
        const comments = document.createTreeWalker(document.body, NodeFilter.SHOW_COMMENT);
        let needsCircuit = false;
        while (comments.nextNode()) {
            if (comments.currentNode.nodeValue?.startsWith('Blazor:') && comments.currentNode.nodeValue.includes('"type":"server"')) needsCircuit = true;
        }
        Blazor.start({circuit: {circuitHandlers: [{onCircuitOpened: () => window.DiaperScoutPwa.markInteractiveReady()}]}}).then(() => {
            if (!needsCircuit) window.DiaperScoutPwa.markInteractiveReady();
            Blazor.addEventListener('enhancedload', () => { syncChrome(); connection(); });
            dispatchEvent(new Event('diaperscout:framework-ready'));
        }).catch(() => failure());
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', startFramework, {once: true});
    else startFramework();
})();
