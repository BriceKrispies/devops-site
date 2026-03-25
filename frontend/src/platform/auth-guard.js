import { initAuth, loadUser, isAuthenticated } from "./auth";
let authInitialized = false;
/**
 * Auth guard — call at the top of every protected page entry point.
 *
 * 1. Fetches /api/auth/config from the backend.
 * 2. If 404 (DevelopmentBypass mode) → skip auth, return true.
 * 3. If authenticated → return true.
 * 4. If not authenticated → redirect to login page.
 *
 * Returns true if the page should proceed rendering, false if redirecting.
 */
export async function requireAuth() {
    // Fetch auth config from backend
    let config;
    try {
        const response = await fetch("/api/auth/config");
        if (response.status === 404) {
            // DevelopmentBypass — no auth required
            return true;
        }
        if (!response.ok) {
            console.error("[auth-guard] Failed to fetch auth config:", response.status);
            return true; // Fail open for now — backend will deny if needed
        }
        config = await response.json();
    }
    catch {
        console.error("[auth-guard] Network error fetching auth config");
        return true; // Fail open — backend will deny unauthorized requests
    }
    if (!config)
        return true;
    // Initialize auth if not already done
    if (!authInitialized) {
        initAuth(config);
        authInitialized = true;
    }
    // Try to load existing session
    await loadUser();
    if (isAuthenticated()) {
        return true;
    }
    // Not authenticated — redirect to login
    const loginUrl = `/src/pages/login.html?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
    window.location.href = loginUrl;
    return false;
}
