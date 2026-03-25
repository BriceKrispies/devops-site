import { initAuth, login, loadUser, isAuthenticated } from "../../platform/auth";
const btn = document.getElementById("login-btn");
const errorEl = document.getElementById("login-error");
async function init() {
    // Fetch auth config from backend
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
        showError("Network error. Please try again.");
        return;
    }
    initAuth(config);
    // Check if already authenticated
    await loadUser();
    if (isAuthenticated()) {
        redirectToReturn();
        return;
    }
    // Wire up login button
    btn.addEventListener("click", async () => {
        btn.disabled = true;
        btn.textContent = "Redirecting...";
        try {
            await login();
        }
        catch (err) {
            showError("Failed to initiate login. Please try again.");
            btn.disabled = false;
            btn.textContent = "Sign in with Okta";
        }
    });
}
function redirectToReturn() {
    const params = new URLSearchParams(window.location.search);
    const returnUrl = params.get("returnUrl") || "/";
    window.location.href = returnUrl;
}
function showError(message) {
    errorEl.textContent = message;
    errorEl.style.display = "block";
}
init();
