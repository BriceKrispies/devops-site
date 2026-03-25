import { initAuth, handleCallback } from "../../platform/auth";
const errorEl = document.getElementById("callback-error");
async function processCallback() {
    // Need to initialize auth before handling callback
    let config;
    try {
        const response = await fetch("/api/auth/config");
        if (response.status === 404) {
            // DevelopmentBypass — redirect to home
            window.location.href = "/";
            return;
        }
        if (!response.ok) {
            showError("Unable to load authentication configuration.");
            return;
        }
        config = await response.json();
    }
    catch {
        showError("Network error during sign-in.");
        return;
    }
    initAuth(config);
    try {
        await handleCallback();
        // Redirect to the originally requested page or home
        const returnUrl = sessionStorage.getItem("auth_return_url") || "/";
        sessionStorage.removeItem("auth_return_url");
        window.location.href = returnUrl;
    }
    catch (err) {
        console.error("[callback] Error processing OIDC callback:", err);
        showError("Sign-in failed. Please try again.");
    }
}
function showError(message) {
    errorEl.textContent = message;
    errorEl.style.display = "block";
}
processCallback();
