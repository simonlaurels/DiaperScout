const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", event => {
    if (event.detail.state === "hide") reconnectModal.close();
    else if (["show", "failed", "rejected", "paused", "resume-failed"].includes(event.detail.state)
        && !reconnectModal.open) reconnectModal.showModal();
});

document.getElementById("components-reconnect-button").addEventListener("click", retry);
document.getElementById("components-resume-button").addEventListener("click", resume);
document.getElementById("components-reload-button").addEventListener("click", () => location.reload());

function showFailure(state) {
    reconnectModal.className = `components-reconnect-${state}`;
    if (!reconnectModal.open) reconnectModal.showModal();
}

async function retry() {
    try {
        if (await Blazor.reconnect()) reconnectModal.close();
        else if (await Blazor.resumeCircuit()) reconnectModal.close();
        else showFailure("rejected");
    } catch {
        showFailure("failed");
    }
}

async function resume() {
    try {
        if (await Blazor.resumeCircuit()) reconnectModal.close();
        else showFailure("rejected");
    } catch {
        showFailure("resume-failed");
    }
}
// A rejected in-memory circuit cannot be recovered by changing visibility or blindly
// reloading. Keep the failure visible and let the user choose when to discard the page.
