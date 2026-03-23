/**
 * Backend API error response contract.
 * Matches the ApiErrorResponse shape returned by the backend.
 */
export interface ApiErrorResponse {
  code: string;
  message: string;
  correlationId: string;
  details?: string | null;
  fieldErrors?: Record<string, string> | null;
}

/** Error kind for UI rendering decisions. */
export type ErrorKind =
  | "validation"
  | "permission"
  | "not-found"
  | "conflict"
  | "unavailable"
  | "rate-limited"
  | "unexpected";

/**
 * Normalized error model for frontend UI consumption.
 * Separates raw transport failures from what gets displayed to users.
 */
export interface NormalizedError {
  title: string;
  message: string;
  correlationId: string | null;
  kind: ErrorKind;
  canRetry: boolean;
  fieldErrors: Record<string, string> | null;
}
