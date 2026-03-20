export interface Service {
  id: string;
  name: string;
  status: "healthy" | "degraded" | "down";
  uptime: string;
  latency: number;
}

export interface Deployment {
  id: string;
  service: string;
  environment: string;
  status: "success" | "failed" | "rolling";
  timestamp: string;
  commit: string;
}

export interface Job {
  id: string;
  name: string;
  pipeline: string;
  status: "running" | "queued" | "passed" | "failed";
  duration: number | null;
  triggeredBy: string;
}

export type TraceSource = "jira" | "github" | "ci";

export interface TraceEvent {
  id: string;
  timestamp: string;
  type: string;
  source: TraceSource;
  summary: string;
  entityIds: Record<string, string>;
  metadata: Record<string, string>;
}
