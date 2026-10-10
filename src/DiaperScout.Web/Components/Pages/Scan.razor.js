let stream;
let frameRequest;
let detectionInFlight = false;
let activeVideo;
let zxingControls;
let zxingModulePromise;
let generation = 0;
let cleanup;
let torchOn = false;
let mountedVideo;
let acquisition = Promise.resolve();

const retailFormats = ["ean_13", "ean_8", "upc_a", "upc_e"];
const zxingAssetPath = "/lib/zxing/zxing-browser.min.js";

export async function start(video, dotnet) {
    stop(video);
    const session = generation;
    activeVideo = video;
    const nativeResult = await startWithNativeDetector(video, dotnet, session);
    if (nativeResult !== "unsupported") return nativeResult;
    if (session !== generation) return "cancelled";

    return startWithZxingFallback(video, dotnet, session);
}

export function mount(video, dotnet) {
    unmount();
    mountedVideo = video;
    const pause = () => {
        if (!activeVideo) return;
        stop(video);
        dotnet.invokeMethodAsync('OnScannerPaused').catch(() => {});
    };
    const visibility = () => { if (document.hidden) pause(); };
    const nav = document.querySelector('.pwa-mobile-nav');
    const measure = () => {
        if (nav && getComputedStyle(nav).display !== 'none')
            video.closest('.scan-page')?.style.setProperty('--scanner-nav-height', `${nav.getBoundingClientRect().height}px`);
    };
    const resize = new ResizeObserver(measure);
    if (nav) resize.observe(nav);
    measure();
    const removed = new MutationObserver(() => { if (!video.isConnected) pause(); });
    removed.observe(document.body, { childList: true, subtree: true });
    document.addEventListener('visibilitychange', visibility);
    window.addEventListener('pagehide', pause);
    cleanup = () => {
        document.removeEventListener('visibilitychange', visibility);
        window.removeEventListener('pagehide', pause);
        resize.disconnect(); removed.disconnect();
    };
    return matchMedia('(max-width: 700px)').matches || matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
}
export function unmount(video) {
    if (video && mountedVideo !== video) return;
    stop(); cleanup?.(); cleanup = undefined; mountedVideo = undefined;
}
export function isInstalled() { return navigator.standalone === true || matchMedia('(display-mode: standalone)').matches; }
export function focusManual() { requestAnimationFrame(() => document.querySelector('#gtin')?.focus()); }
export function canTorch() { try { return stream?.getVideoTracks()[0]?.getCapabilities?.().torch === true; } catch { return false; } }
export async function toggleTorch() {
    if (!canTorch()) return false;
    const track = stream.getVideoTracks()[0];
    const session = generation;
    try { await track.applyConstraints({ advanced: [{ torch: !torchOn }] }); if (session !== generation) return false; torchOn = !torchOn; } catch { }
    return torchOn;
}

async function acquire(video, session) {
    const previous = acquisition;
    let release;
    acquisition = new Promise(resolve => { release = resolve; });
    await previous;
    try {
        if (session !== generation || !video.isConnected || document.hidden) return false;
        const acquired = await navigator.mediaDevices.getUserMedia({ audio: false, video: { facingMode: { ideal: 'environment' } } });
        if (session !== generation || !video.isConnected || document.hidden) {
            acquired.getTracks().forEach(track => track.stop());
            return false;
        }
        stream = acquired; video.srcObject = stream;
        return true;
    }
    finally { release(); }
}

async function startWithNativeDetector(video, dotnet, session) {
    if (!("BarcodeDetector" in globalThis) || !navigator.mediaDevices?.getUserMedia) {
        return "unsupported";
    }

    let supportedFormats;
    try {
        supportedFormats = await BarcodeDetector.getSupportedFormats();
    } catch {
        return "unsupported";
    }

    const formats = retailFormats.filter(format => supportedFormats.includes(format));
    if (session !== generation) return "cancelled";
    if (formats.length === 0) return "unsupported";

    let detector;
    try {
        detector = new BarcodeDetector({ formats });
        if (!await acquire(video, session)) return "cancelled";
    } catch (error) {
        if (session === generation) stop(video);
        return cameraErrorStatus(error);
    }

    try {
        await video.play();
    } catch {
        if (session === generation) stop(video);
        return "error";
    }
    if (session !== generation) return "cancelled";

    const detect = async () => {
        if (session !== generation) return;
        if (!stream || video.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
            frameRequest = requestAnimationFrame(detect);
            return;
        }

        if (!detectionInFlight) {
            detectionInFlight = true;
            try {
                const matches = await detector.detect(video);
                if (session !== generation) return;
                const gtin = matches.map(match => normaliseGtin(match.format === 'upc_e' ? expandUpcE(match.rawValue) : match.rawValue)).find(Boolean);
                if (gtin) {
                    stop(video);
                    await dotnet.invokeMethodAsync("OnBarcodeDetected", gtin);
                    return;
                }
            } catch {
                if (session !== generation) return;
                stop(video);
                await dotnet.invokeMethodAsync("OnScannerError");
                return;
            } finally {
                detectionInFlight = false;
            }
        }

        if (stream) frameRequest = requestAnimationFrame(detect);
    };

    frameRequest = requestAnimationFrame(detect);
    return "started";
}

async function startWithZxingFallback(video, dotnet, session) {
    if (!navigator.mediaDevices?.getUserMedia) return "unsupported";

    let zxing;
    try {
        zxing = await getZxing();
    } catch {
        return "unsupported";
    }

    if (!zxing?.BrowserMultiFormatOneDReader) return "unsupported";
    if (session !== generation) return "cancelled";

    let completed = false;
    try {
        const reader = new zxing.BrowserMultiFormatOneDReader();
        if (!await acquire(video, session)) return "cancelled";
        const controls = await reader.decodeFromStream(
            stream,
            video,
            async (result, _error, controls) => {
                if (completed || session !== generation) return;
                if (result) {
                    // The pinned ZXing bundle uses BarcodeFormat.UPC_E = 15.
                    const gtin = normaliseGtin(result.getBarcodeFormat?.() === 15 ? expandUpcE(result.getText()) : result.getText());
                    if (!gtin) return;

                    completed = true;
                    stop(video, controls);
                    await dotnet.invokeMethodAsync("OnBarcodeDetected", gtin);
                    return;
                }

                // The continuous ZXing reader owns retry-versus-terminal decisions for its
                // per-frame errors. A callback error is not enough to classify a scanner failure.
            });
        if (session !== generation) { stopControls(controls); return "cancelled"; }
        zxingControls = controls;
        return "started";
    } catch (error) {
        if (session === generation) stop(video);
        return cameraErrorStatus(error);
    }
}

async function getZxing() {
    if (globalThis.ZXingBrowser) return globalThis.ZXingBrowser;

    zxingModulePromise ??= import(zxingAssetPath);
    await zxingModulePromise;
    return globalThis.ZXingBrowser;
}

function normaliseGtin(value) {
    const gtin = value?.replace(/[\s-]/g, "");
    return /^\d{8,14}$/.test(gtin) ? gtin : undefined;
}
export function expandUpcE(value) {
    if (!/^[01]\d{7}$/.test(value || '')) return value;
    const [ns,a,b,c,d,e,f,check]=value;
    if('012'.includes(f))return ns+a+b+f+'0000'+c+d+e+check;
    if(f==='3')return ns+a+b+c+'00000'+d+e+check;
    if(f==='4')return ns+a+b+c+d+'00000'+e+check;
    return ns+a+b+c+d+e+'0000'+f+check;
}

function cameraErrorStatus(error) {
    if (error?.name === "NotAllowedError" || error?.name === "SecurityError") return "denied";
    if (error?.name === "NotFoundError" || error?.name === "OverconstrainedError") return "unavailable";
    return "error";
}

export function stop(video, controls) {
    if (video && activeVideo && video !== activeVideo) { stopControls(controls); return; }
    generation++;
    torchOn = false;
    if (frameRequest) cancelAnimationFrame(frameRequest);
    frameRequest = undefined;
    detectionInFlight = false;

    stopControls(controls);
    if (zxingControls && zxingControls !== controls) stopControls(zxingControls);
    zxingControls = undefined;

    stream?.getTracks().forEach(track => track.stop());
    stream = undefined;

    const target = video || activeVideo;
    if (target) {
        // ZXing attaches its stream to the video before its async controls resolve.
        target.srcObject?.getTracks?.().forEach(track => track.stop());
        target.pause();
        target.srcObject = null;
    }
    activeVideo = undefined;
}
function stopControls(controls) {
    // Some devices return an async stop while their torch is being disabled.
    try { controls?.stop()?.catch?.(() => {}); } catch { }
}
