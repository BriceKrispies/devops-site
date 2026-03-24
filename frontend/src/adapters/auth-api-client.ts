import { apiFetch, type ApiResult } from "./api-client";
import { getAccessToken } from "../platform/auth";

/**
 * Auth-aware fetch wrapper. Injects the Bearer token from the current OIDC session.
 * Falls back to standard apiFetch if no token is available (DevelopmentBypass mode).
 */
export async function authApiFetch<T>(
  url: string,
  options: RequestInit = {},
): Promise<ApiResult<T>> {
  const token = getAccessToken();

  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  return apiFetch<T>(url, { ...options, headers });
}
