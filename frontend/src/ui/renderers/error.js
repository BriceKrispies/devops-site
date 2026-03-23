/**
 * Render an error banner for display inside a region.
 * Shows a safe user-facing message, correlation ID for support, and optional retry.
 */
export function renderError(error, onRetryAttr) {
    const correlationHtml = error.correlationId
        ? `<span class="error-correlation">Ref: <code>${escapeHtml(error.correlationId)}</code></span>`
        : "";
    const retryHtml = error.canRetry && onRetryAttr
        ? `<button class="error-retry-btn" ${onRetryAttr}>Retry</button>`
        : "";
    const fieldErrorsHtml = renderFieldErrors(error.fieldErrors);
    return `<div class="error-banner error-banner--${escapeHtml(error.kind)}">
  <div class="error-banner-header">
    <span class="error-banner-title">${escapeHtml(error.title)}</span>
    ${correlationHtml}
  </div>
  <p class="error-banner-message">${escapeHtml(error.message)}</p>
  ${fieldErrorsHtml}
  ${retryHtml}
</div>`;
}
/**
 * Render a simple error string (legacy fallback for plain error messages).
 */
export function renderSimpleError(message) {
    return `<div class="region-error"><p>${escapeHtml(message)}</p></div>`;
}
/** Render field-level validation errors as a list. */
function renderFieldErrors(fieldErrors) {
    if (!fieldErrors)
        return "";
    const entries = Object.entries(fieldErrors);
    if (entries.length === 0)
        return "";
    const items = entries
        .map(([field, msg]) => `<li><strong>${escapeHtml(field)}</strong>: ${escapeHtml(msg)}</li>`)
        .join("");
    return `<ul class="error-field-list">${items}</ul>`;
}
function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}
