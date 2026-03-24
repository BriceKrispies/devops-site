import { UserManager, WebStorageStateStore, type User, type UserManagerSettings } from "oidc-client-ts";

/** OIDC config returned by the backend at /api/auth/config */
export interface AuthConfig {
  authority: string;
  clientId: string;
  scope: string;
}

type AuthStateListener = (user: User | null) => void;

let userManager: UserManager | null = null;
let currentUser: User | null = null;
const listeners: Set<AuthStateListener> = new Set();

function notifyListeners(): void {
  for (const listener of listeners) {
    listener(currentUser);
  }
}

/**
 * Initialize the OIDC UserManager with config from the backend.
 * Must be called before any other auth function.
 */
export function initAuth(config: AuthConfig): void {
  const settings: UserManagerSettings = {
    authority: config.authority,
    client_id: config.clientId,
    redirect_uri: `${window.location.origin}/src/pages/callback.html`,
    post_logout_redirect_uri: `${window.location.origin}/src/pages/login.html`,
    response_type: "code",
    scope: config.scope,
    automaticSilentRenew: true,
    userStore: new WebStorageStateStore({ store: sessionStorage }),
  };

  userManager = new UserManager(settings);

  userManager.events.addUserLoaded((user) => {
    currentUser = user;
    notifyListeners();
  });

  userManager.events.addUserUnloaded(() => {
    currentUser = null;
    notifyListeners();
  });

  userManager.events.addSilentRenewError((error) => {
    console.error("[auth] Silent renew error:", error);
  });
}

/** Redirect the user to the OIDC login page. */
export async function login(): Promise<void> {
  if (!userManager) throw new Error("Auth not initialized. Call initAuth() first.");
  await userManager.signinRedirect();
}

/** Handle the OIDC callback after redirect from the identity provider. */
export async function handleCallback(): Promise<User> {
  if (!userManager) throw new Error("Auth not initialized. Call initAuth() first.");
  const user = await userManager.signinRedirectCallback();
  currentUser = user;
  notifyListeners();
  return user;
}

/** Log out the user. */
export async function logout(): Promise<void> {
  if (!userManager) throw new Error("Auth not initialized. Call initAuth() first.");
  await userManager.signoutRedirect();
}

/** Load the user from session storage (no redirect). */
export async function loadUser(): Promise<User | null> {
  if (!userManager) return null;
  const user = await userManager.getUser();
  currentUser = user;
  return user;
}

/** Get the current access token, or null if not authenticated. */
export function getAccessToken(): string | null {
  return currentUser?.access_token ?? null;
}

/** Check if the user is currently authenticated. */
export function isAuthenticated(): boolean {
  return currentUser !== null && !currentUser.expired;
}

/** Get the user's email from the token claims. */
export function getUserEmail(): string | null {
  if (!currentUser?.profile) return null;
  return (currentUser.profile.email as string) ?? null;
}

/** Subscribe to auth state changes. Returns an unsubscribe function. */
export function onAuthStateChange(listener: AuthStateListener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
