import { apiFetch } from "./api-client";
import { getAccessToken } from "../platform/auth";
/**
 * Auth-aware fetch wrapper. Injects the Bearer token from the current OIDC session.
 * Falls back to standard apiFetch if no token is available (DevelopmentBypass mode).
 */
export async function authApiFetch(url, options = {}) {
    const token = getAccessToken();
    const headers = {
        ...options.headers,
    };
    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }
    return apiFetch(url, { ...options, headers });
}
