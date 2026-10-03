// Stable PlaceCategory codes; only saved categories select a pictogram.
const categories = {
    1: ['pharmacy','Pharmacy','<rect x="15" y="19" width="18" height="8" rx="4" transform="rotate(-40 24 23)"/><path d="m21 20 6 6"/>'],
    2: ['supermarket','Supermarket','<path d="m14 20 3 11h14l3-11Zm5 0 3-5m4 0 3 5M20 24v4m8-4v4"/>'],
    3: ['specialist','Specialist retailer','<path d="M15 21h18v11H15Zm0-4h18v4H15m4 4h5v4h-5m9-5v5"/>'],
    4: ['retailer','General retailer','<path d="M15 20h18v12H15m-1-12 3-5h14l3 5M19 32v-7h5v7m4-8h3"/>'],
    5: ['convenience','Convenience store','<path d="M16 20h16l1 12H15Zm4 0v-3a4 4 0 0 1 8 0v3m-7 6h6"/>'],
    6: ['other','Other','<circle cx="17" cy="23" r="1.5"/><circle cx="24" cy="23" r="1.5"/><circle cx="31" cy="23" r="1.5"/>']
};
const neutral = ['neutral',null,'<path d="m16 19 8-4 8 4v10l-8 4-8-4Z"/><path d="m16 19 8 4 8-4M24 23v10m-4-16 8 4"/>'];
const definition = category => Number.isInteger(category) ? categories[category] ?? neutral : neutral;
export function categoryLabel(category) { return definition(category)[1]; }
export function discoveryMarker(observationCount, category) {
    const count = Math.max(1, Math.floor(Number(observationCount) || 1));
    const [kind,,pictogram] = definition(category);
    return `<svg class="atlas-marker-art atlas-category-${kind}" viewBox="0 0 48 58" aria-hidden="true"><path class="atlas-marker-shadow" d="M24 56 9 36C-5 18 7 2 24 2s29 16 15 34Z"/><path class="atlas-marker-body" d="M24 53 10 34C-2 18 9 3 24 3s26 15 14 31Z"/><circle cx="24" cy="23" r="14" class="atlas-marker-inset"/><g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round">${pictogram}</g></svg>${count > 1 ? `<span class="atlas-marker-count" aria-hidden="true">${count > 99 ? '99+' : count}</span>` : ''}`;
}
