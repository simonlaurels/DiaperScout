export function isStandalone() { return matchMedia('(display-mode: standalone)').matches || navigator.standalone === true; }
export function locate() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) { reject(new Error('Location unavailable')); return; }
        navigator.geolocation.getCurrentPosition(p => resolve({latitude: p.coords.latitude, longitude: p.coords.longitude, accuracy: p.coords.accuracy}), reject,
            {timeout: 10000, maximumAge: 60000, enableHighAccuracy: false});
    });
}
