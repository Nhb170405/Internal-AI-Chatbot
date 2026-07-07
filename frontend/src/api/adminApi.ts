import { apiRequest } from "./httpClient";
import type { AdminAuditLogQuery, AdminAuditLogsResponse, AdminOverviewResponse, AdminUsageResponse, AdminUserItem } from "../types/admin";

export function getAdminOverview() {
  return apiRequest<AdminOverviewResponse>("/api/admin/overview");
}

export function getAdminUsers() {
  return apiRequest<AdminUserItem[]>("/api/admin/users");
}

export function getAdminUsage(query: { from?: string; to?: string } = {}) {
  const params = new URLSearchParams();

  if (query.from) {
    params.set("from", query.from);
  }

  if (query.to) {
    params.set("to", query.to);
  }

  const suffix = params.toString() ? `?${params.toString()}` : "";
  return apiRequest<AdminUsageResponse>(`/api/admin/usage${suffix}`);
}

export function getAdminAuditLogs(query: AdminAuditLogQuery = {}) {
  const params = new URLSearchParams();

  if (query.action) {
    params.set("action", query.action);
  }

  if (query.resourceType) {
    params.set("resourceType", query.resourceType);
  }

  if (query.actorId) {
    params.set("actorId", query.actorId);
  }

  if (query.page) {
    params.set("page", String(query.page));
  }

  if (query.pageSize) {
    params.set("pageSize", String(query.pageSize));
  }

  const suffix = params.toString() ? `?${params.toString()}` : "";
  return apiRequest<AdminAuditLogsResponse>(`/api/admin/audit-logs${suffix}`);
}
