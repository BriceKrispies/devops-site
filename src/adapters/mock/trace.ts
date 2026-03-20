import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../state/store.types";
import type { TraceEvent } from "../../types/models";
import type { TopicPayload } from "../../state/topics";
import { TOPICS } from "../../state/topics";

const MOCK_EVENTS: TraceEvent[] = [
  {
    id: "evt-01",
    timestamp: "2026-03-20T09:12:00Z",
    type: "ticket.created",
    source: "jira",
    summary: "OPS-1234 created: Investigate api-gateway latency spike",
    entityIds: { ticket: "OPS-1234", service: "api-gateway" },
    metadata: { priority: "P1", assignee: "oncall" },
  },
  {
    id: "evt-02",
    timestamp: "2026-03-20T09:45:00Z",
    type: "pr.opened",
    source: "github",
    summary: "PR #891 opened: Fix connection pool exhaustion",
    entityIds: { pr: "891", ticket: "OPS-1234", service: "api-gateway", repo: "infra/api-gateway" },
    metadata: { author: "dev-a", branch: "fix/conn-pool" },
  },
  {
    id: "evt-03",
    timestamp: "2026-03-20T10:02:00Z",
    type: "pipeline.started",
    source: "ci",
    summary: "CI pipeline started for fix/conn-pool",
    entityIds: { pipeline: "api-gw-build-447", pr: "891", service: "api-gateway" },
    metadata: { commit: "a3f8c21", trigger: "push" },
  },
  {
    id: "evt-04",
    timestamp: "2026-03-20T10:08:00Z",
    type: "pipeline.passed",
    source: "ci",
    summary: "CI pipeline passed — all checks green",
    entityIds: { pipeline: "api-gw-build-447", pr: "891", service: "api-gateway" },
    metadata: { commit: "a3f8c21", duration: "362s" },
  },
  {
    id: "evt-05",
    timestamp: "2026-03-20T10:15:00Z",
    type: "pr.approved",
    source: "github",
    summary: "PR #891 approved by dev-b",
    entityIds: { pr: "891", ticket: "OPS-1234", service: "api-gateway" },
    metadata: { reviewer: "dev-b" },
  },
  {
    id: "evt-06",
    timestamp: "2026-03-20T10:18:00Z",
    type: "pr.merged",
    source: "github",
    summary: "PR #891 merged into main",
    entityIds: { pr: "891", ticket: "OPS-1234", service: "api-gateway", commit: "a3f8c21" },
    metadata: { mergedBy: "dev-a" },
  },
  {
    id: "evt-07",
    timestamp: "2026-03-20T10:20:00Z",
    type: "deploy.started",
    source: "ci",
    summary: "Deploy api-gateway to staging",
    entityIds: { deploy: "deploy-stg-221", service: "api-gateway", commit: "a3f8c21" },
    metadata: { environment: "staging" },
  },
  {
    id: "evt-08",
    timestamp: "2026-03-20T10:24:00Z",
    type: "deploy.completed",
    source: "ci",
    summary: "Deploy api-gateway to staging succeeded",
    entityIds: { deploy: "deploy-stg-221", service: "api-gateway", commit: "a3f8c21" },
    metadata: { environment: "staging", duration: "238s" },
  },
  {
    id: "evt-09",
    timestamp: "2026-03-20T10:40:00Z",
    type: "ticket.transitioned",
    source: "jira",
    summary: "OPS-1234 moved to In Review",
    entityIds: { ticket: "OPS-1234", service: "api-gateway" },
    metadata: { from: "In Progress", to: "In Review" },
  },
  {
    id: "evt-10",
    timestamp: "2026-03-20T11:00:00Z",
    type: "deploy.started",
    source: "ci",
    summary: "Deploy api-gateway to production",
    entityIds: { deploy: "deploy-prod-114", service: "api-gateway", commit: "a3f8c21" },
    metadata: { environment: "production" },
  },
  {
    id: "evt-11",
    timestamp: "2026-03-20T11:05:00Z",
    type: "deploy.completed",
    source: "ci",
    summary: "Deploy api-gateway to production succeeded",
    entityIds: { deploy: "deploy-prod-114", service: "api-gateway", commit: "a3f8c21" },
    metadata: { environment: "production", duration: "295s" },
  },
  {
    id: "evt-12",
    timestamp: "2026-03-20T11:30:00Z",
    type: "ticket.closed",
    source: "jira",
    summary: "OPS-1234 closed — latency resolved",
    entityIds: { ticket: "OPS-1234", service: "api-gateway" },
    metadata: { resolution: "Fixed" },
  },
];

export const mockTraceAdapter: DataAdapter = {
  start(store: Store) {
    const payload: TopicPayload<TraceEvent[]> = { status: "ok", data: MOCK_EVENTS };
    store.publish(TOPICS.TRACE_DATA, payload);
  },
  stop() {},
};
