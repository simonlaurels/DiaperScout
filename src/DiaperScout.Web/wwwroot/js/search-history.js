const key = 'diaperscout.search.recent.v1';
export function read() {
    try {
        const values = JSON.parse(localStorage.getItem(key) || '[]');
        return Array.isArray(values) ? values.filter(x => typeof x === 'string' && x.trim().length > 0 && x.length <= 200).slice(0, 6) : [];
    } catch { return []; }
}
export function write(values) {
    try { localStorage.setItem(key, JSON.stringify(values.slice(0, 6))); } catch { /* Search still works when browser storage is unavailable. */ }
}
