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

// Acknowledge Welcome actions before the existing passkey/link handlers run.
// Authentication and navigation remain owned by their existing handlers.
let pendingAction;
let pendingContents;
function resetAuthenticationFeedback() {
    if (pendingAction) {
        pendingAction.replaceChildren(...pendingContents);
        pendingAction.removeAttribute('aria-busy');
        pendingAction.removeAttribute('aria-disabled');
    }
    pendingAction = pendingContents = undefined;
}
document.addEventListener('click', event => {
    const action = event.target.closest?.('#pwa-welcome .welcome-primary, #pwa-welcome .welcome-secondary');
    if (!action || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    if (pendingAction) { event.preventDefault(); event.stopImmediatePropagation(); return; }
    pendingAction = action;
    pendingContents = [...action.childNodes];
    const spinner = document.createElement('span');
    spinner.className = 'welcome-auth-spinner';
    spinner.setAttribute('aria-hidden', 'true');
    action.replaceChildren(spinner, document.createTextNode(action.matches('.welcome-secondary') ? 'Signing in…' : 'Getting ready…'));
    action.setAttribute('aria-busy', 'true');
    action.setAttribute('aria-disabled', 'true');
}, true);
document.addEventListener('diaperscout:welcome-auth-settled', event => {
    // Successful authentication starts navigation; keep feedback during that wait.
    if (!event.detail.navigating && pendingAction?.matches('.welcome-secondary')) resetAuthenticationFeedback();
});
window.addEventListener('pageshow', resetAuthenticationFeedback);
window.addEventListener('diaperscout:framework-ready', () => {
    Blazor.addEventListener('enhancedload', resetAuthenticationFeedback);
});
if (window.Blazor?.addEventListener) Blazor.addEventListener('enhancedload', resetAuthenticationFeedback);
