const supported = () => window.isSecureContext && !!window.PublicKeyCredential && !!navigator.credentials;
const fromBase64Url = value => Uint8Array.from(atob(value.replace(/-/g, "+").replace(/_/g, "/")), c => c.charCodeAt(0));
const toBase64Url = value => btoa(String.fromCharCode(...new Uint8Array(value))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");

async function request(root, url, body, method = "POST") {
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const response = await fetch(url, {
        method, credentials: "same-origin", headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token ?? "" },
        body: body === undefined ? undefined : JSON.stringify(body)
    });
    if (response.redirected) throw new Error("Please sign in again, then return to Passkeys.");
    if (!response.ok) {
        const error = await response.json().catch(() => null);
        throw new Error(error?.message ?? (response.status === 429 ? "Too many attempts. Please wait a minute and try again." : "The request could not be completed. Please try again."));
    }
    return response.status === 204 || response.headers.get("content-length") === "0" ? null : response.json().catch(() => null);
}

function credentialJson(credential) {
    const response = credential.response;
    const result = {
        id: credential.id, rawId: toBase64Url(credential.rawId), type: credential.type,
        clientExtensionResults: credential.getClientExtensionResults(),
        response: { clientDataJSON: toBase64Url(response.clientDataJSON) }
    };
    if (response.attestationObject) {
        result.response.attestationObject = toBase64Url(response.attestationObject);
        result.response.transports = response.getTransports?.() ?? [];
    } else {
        result.response.authenticatorData = toBase64Url(response.authenticatorData);
        result.response.signature = toBase64Url(response.signature);
        result.response.userHandle = response.userHandle ? toBase64Url(response.userHandle) : null;
    }
    return result;
}

async function loadPasskeys(root) {
    const list = root.querySelector("[data-passkey-list]");
    const passkeys = await request(root, "/account/passkeys/list", undefined, "GET");
    list.replaceChildren();
    if (root.hasAttribute('data-backpack-account')) {
        const {renderBackpackPasskeys} = await import('./backpack.js');
        renderBackpackPasskeys(list, passkeys ?? []);
        return;
    }
    if (!passkeys?.length) { list.textContent = "You haven’t added a passkey yet."; return; }
    for (const passkey of passkeys) {
        const row = document.createElement("div");
        row.className = "ds-passkey-item";
        const details = document.createElement("div");
        const name = document.createElement("strong");
        name.textContent = passkey.name;
        const dates = document.createElement("small");
        dates.textContent = `Added ${new Date(passkey.createdAtUtc).toLocaleDateString()} · ${passkey.lastUsedAtUtc ? `Last used ${new Date(passkey.lastUsedAtUtc).toLocaleDateString()}` : "Not used yet"}`;
        details.append(name, dates);
        const button = document.createElement("button");
        button.type = "button";
        button.className = "ds-button";
        button.dataset.passkeyAction = "remove";
        button.dataset.passkeyId = passkey.id;
        button.textContent = "Remove";
        button.setAttribute("aria-label", `Remove ${passkey.name}`);
        row.append(details, button);
        list.append(row);
    }
}

document.addEventListener("click", async event => {
    const button = event.target.closest?.("[data-passkey-action]");
    if (!button) return;
    const root = button.closest("[data-passkey-account], [data-passkey-signin]");
    if (!root || root.dataset.busy) return;
    const message = root.querySelector("[data-passkey-message]");
    const action = button.dataset.passkeyAction;
    if (action === "remove" && !window.confirm("Remove this passkey? You can still sign in with an email link or another passkey.")) return;
    root.dataset.busy = "true";
    root.querySelectorAll("button").forEach(item => item.disabled = true);
    message.textContent = "";
    let navigating = false;
    try {
        if (action === "remove") {
            await request(root, `/account/passkeys/${button.dataset.passkeyId}`, undefined, "DELETE");
            await loadPasskeys(root);
            message.textContent = "Passkey removed.";
            return;
        }
        if (!supported()) throw new Error("This browser doesn’t support passkeys here. You can still use an email sign-in link.");
        const registration = action === "register";
        const name = registration ? root.querySelector("#passkey-name").value.trim() : undefined;
        if (registration && !name) throw new Error("Give this passkey a name so you can recognise it later.");
        const base = registration ? "/account/passkeys" : "/signin/passkey";
        const options = await request(root, `${base}/options`, {});
        const publicKey = options.publicKey;
        publicKey.challenge = fromBase64Url(publicKey.challenge);
        if (registration) {
            publicKey.user.id = fromBase64Url(publicKey.user.id);
            publicKey.excludeCredentials = (publicKey.excludeCredentials ?? []).map(item => ({ ...item, id: fromBase64Url(item.id) }));
        } else {
            publicKey.allowCredentials = (publicKey.allowCredentials ?? []).map(item => ({ ...item, id: fromBase64Url(item.id) }));
        }
        message.textContent = "Follow your device’s instructions to continue.";
        const credential = await navigator.credentials[registration ? "create" : "get"]({ publicKey });
        if (!credential) throw new Error("No passkey was selected. Please try again or use an email sign-in link.");
        const result = await request(root, `${base}/verify`, { requestId: options.requestId, credential: credentialJson(credential), name });
        if (registration) {
            await loadPasskeys(root);
            root.querySelector("#passkey-name").value = "";
            message.textContent = "Passkey added. You can now use it to sign in.";
        } else { window.location.assign(result.redirect); navigating = true; }
    } catch (error) {
        const fallback = root.querySelector("[data-passkey-fallback]");
        if (fallback) fallback.hidden = false;
        message.textContent = error.name === "NotAllowedError" ? "The passkey request was cancelled or no matching passkey was available. Try again or use an email sign-in link."
            : error.name === "InvalidStateError" ? "This device already has a passkey for your account. Try another device or use your existing passkey."
            : error.message || "Passkey sign-in wasn’t completed. You can use an email sign-in link instead.";
    } finally {
        delete root.dataset.busy;
        root.querySelectorAll("button").forEach(item => item.disabled = false);
        if (root.closest('#pwa-welcome')) root.dispatchEvent(new CustomEvent('diaperscout:welcome-auth-settled', { bubbles: true, detail: { navigating } }));
    }
});

function initialise() {
    document.querySelectorAll("[data-passkey-signin], [data-passkey-account]").forEach(root => {
        if (root.hasAttribute('data-backpack-account') && !(matchMedia('(display-mode: standalone)').matches || navigator.standalone === true)) return;
        // Enhanced navigation can preserve this root while replacing its list with SSR
        // markup. Rehydrate that list even when the root was initialised previously.
        const resetList = root.querySelector("[data-passkey-list]")?.textContent.trim() === "Loading your passkeys…";
        if (root.dataset.initialised && !resetList) return;
        root.dataset.initialised = "true";
        if (!supported()) {
            root.querySelector("[data-passkey-message]").textContent = "Passkeys aren’t supported in this browser. Email sign-in links are still available.";
            const fallback = root.querySelector("[data-passkey-fallback]");
            if (fallback) fallback.hidden = false;
        }
        if (root.matches("[data-passkey-account]")) loadPasskeys(root).catch(error => {
            root.querySelector("[data-passkey-list]").textContent = "Your passkeys could not be loaded. Refresh the page to try again.";
            root.querySelector("[data-passkey-message]").textContent = error.message;
        });
    });
}
initialise();
if (typeof window.Blazor?.addEventListener === "function") Blazor.addEventListener("enhancedload", initialise);
else addEventListener("diaperscout:framework-ready", () => Blazor.addEventListener("enhancedload", initialise), { once: true });
