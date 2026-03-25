import { isAuthenticated, logout } from "../platform/auth";
/**
 * Wire up the sign-out button in the topbar.
 * Shows the button only when the user is authenticated via OIDC.
 */
export function initSignout() {
    const btn = document.getElementById("signout-btn");
    if (!btn)
        return;
    if (isAuthenticated()) {
        btn.style.display = "";
        btn.addEventListener("click", async () => {
            try {
                await logout();
            }
            catch (err) {
                console.error("[signout] Logout failed:", err);
                // Fallback: clear session and redirect to login
                sessionStorage.clear();
                window.location.href = "/src/pages/login.html";
            }
        });
    }
}
