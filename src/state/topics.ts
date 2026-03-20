import type { Service, Deployment, Job, TraceEvent } from "../types/models";

export const TOPICS = {
  SERVICES_HEALTH: "services.health",
  DEPLOYMENTS_RECENT: "deployments.recent",
  JOBS_ACTIVE: "jobs.active",
  TRACE_QUERY: "trace.query",
  TRACE_RESULTS: "trace.results",
  TRACE_DATA: "trace.data",
} as const;

export type TopicName = (typeof TOPICS)[keyof typeof TOPICS];

export interface TopicPayload<T> {
  status: "ok" | "error";
  data?: T;
  error?: string;
}

export type ServicesPayload = TopicPayload<Service[]>;
export type DeploymentsPayload = TopicPayload<Deployment[]>;
export type JobsPayload = TopicPayload<Job[]>;
export type TraceResultsPayload = TopicPayload<TraceEvent[]>;
