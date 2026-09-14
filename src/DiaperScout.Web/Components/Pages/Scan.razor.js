let stream;
let frameRequest;
let detectionInFlight = false;
let activeVideo;
let zxingControls;
let zxingModulePromise;

const retailFormats = ["ean_13", "ean_8", "upc_a", "upc_e"];
const zxingAssetPath = "/lib/zxing/zxing-browser.min.js";

export async function start(video, dotnet) {
    stop(video);

    const nativeResult = await startWithNativeDetector(video, dotnet);
    if (nativeResult !== "unsupported") return nativeResult;

    return startWithZxingFallback(video, dotnet);
}

async function startWithNativeDetector(video, dotnet) {
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
    if (formats.length === 0) return "unsupported";

    let detector;
    try {
        detector = new BarcodeDetector({ formats });
        stream = await navigator.mediaDevices.getUserMedia({
            audio: false,
            video: { facingMode: { ideal: "environment" } }
        });
    } catch (error) {
        return cameraErrorStatus(error);
    }

    activeVideo = video;
    video.srcObject = stream;
    try {
        await video.play();
    } catch {
        stop(video);
        return "error";
    }

    const detect = async () => {
        if (!stream || video.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
            frameRequest = requestAnimationFrame(detect);
            return;
        }

        if (!detectionInFlight) {
            detectionInFlight = true;
            try {
                const matches = await detector.detect(video);
                const gtin = matches.map(match => normaliseGtin(match.rawValue)).find(Boolean);
                if (gtin) {
                    stop(video);
                    await dotnet.invokeMethodAsync("OnBarcodeDetected", gtin);
                    return;
                }
            } catch {
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

async function startWithZxingFallback(video, dotnet) {
    if (!navigator.mediaDevices?.getUserMedia) return "unsupported";

    let zxing;
    try {
        zxing = await getZxing();
    } catch {
        return "unsupported";
    }

    if (!zxing?.BrowserMultiFormatOneDReader) return "unsupported";

    let completed = false;
    try {
        const reader = new zxing.BrowserMultiFormatOneDReader();
        zxingControls = await reader.decodeFromConstraints(
            {
                audio: false,
                video: { facingMode: { ideal: "environment" } }
            },
            video,
            async (result, _error, controls) => {
                if (completed) return;
                if (result) {
                    const gtin = normaliseGtin(result.getText());
                    if (!gtin) return;

                    completed = true;
                    stop(video, controls);
                    await dotnet.invokeMethodAsync("OnBarcodeDetected", gtin);
                    return;
                }

                // The continuous ZXing reader owns retry-versus-terminal decisions for its
                // per-frame errors. A callback error is not enough to classify a scanner failure.
            });
        return "started";
    } catch (error) {
        stop(video);
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

function cameraErrorStatus(error) {
    if (error?.name === "NotAllowedError" || error?.name === "SecurityError") return "denied";
    if (error?.name === "NotFoundError" || error?.name === "OverconstrainedError") return "unavailable";
    return "error";
}

export function stop(video, controls) {
    if (frameRequest) cancelAnimationFrame(frameRequest);
    frameRequest = undefined;
    detectionInFlight = false;

    controls?.stop();
    if (zxingControls && zxingControls !== controls) zxingControls.stop();
    zxingControls = undefined;

    stream?.getTracks().forEach(track => track.stop());
    stream = undefined;

    const target = video || activeVideo;
    if (target) {
        target.pause();
        target.srcObject = null;
    }
    activeVideo = undefined;
}
