// Only an onboarding preference is stored. Authentication is determined by
// server-rendered AuthorizeView; authenticated documents have no welcome panel.
const key = 'ds-welcome-complete-v1';
let completed = false;
try { completed = localStorage.getItem(key) === 'yes'; } catch { /* Storage may be unavailable. */ }

function syncWelcome() {
    const panel = document.getElementById('pwa-welcome');
    const show = !!panel && location.pathname === '/' &&
        document.documentElement.dataset.standalone === 'true' &&
        document.documentElement.dataset.pwaState === 'ready' && !completed;
    document.documentElement.toggleAttribute('data-welcome-active', show);
    if (panel) {
        const wasHidden = panel.hidden;
        panel.hidden = !show;
        if (show && wasHidden) panel.querySelector('h1').focus({ preventScroll: true });
    }
}

document.addEventListener('click', event => {
    if (!event.target.closest('#welcome-continue')) return;
    completed = true;
    try { localStorage.setItem(key, 'yes'); } catch { /* Continue still works for this document. */ }
    syncWelcome();
    const heading = document.querySelector('.explore-page h1');
    if (heading) { heading.tabIndex = -1; heading.focus({ preventScroll: true }); }
});
window.addEventListener('diaperscout:ready', syncWelcome);
let navigationBound = false;
function bindNavigation() {
    if (!navigationBound && window.Blazor?.addEventListener) {
        Blazor.addEventListener('enhancedload', syncWelcome);
        navigationBound = true;
    }
    syncWelcome();
}
window.addEventListener('diaperscout:framework-ready', bindNavigation);
bindNavigation();
