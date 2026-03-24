import type { AuthConfig } from "../../platform/auth";
import { initAuth, handleCallback } from "../../platform/auth";

const errorEl = document.getElementById("callback-error") as HTMLElement;

async function processCallback(): Promise<void> {
  // Need to initialize auth before handling callback
  let config: AuthConfig;
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
  } catch {
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
  } catch (err) {
    console.error("[callback] Error processing OIDC callback:", err);
    showError("Sign-in failed. Please try again.");
  }
}

function showError(message: string): void {
  errorEl.textContent = message;
  errorEl.style.display = "block";
}

processCallback();
