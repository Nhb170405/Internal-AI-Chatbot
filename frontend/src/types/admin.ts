export type AdminAuditLogItem = {
  id: string;
  actorUserId: string | null;
  actorGuestSessionId: string | null;
  action: string;
  resourceType: string;
  resourceId: string | null;
  metadataJson: string | null;
  ipAddress: string | null;
  createdAt: string;
};

export type AdminAuditLogsResponse = {
  page: number;
  pageSize: number;
  totalCount: number;
  items: AdminAuditLogItem[];
};

export type AdminAuditLogQuery = {
  action?: string;
  resourceType?: string;
  actorId?: string;
  page?: number;
  pageSize?: number;
};

export type AdminOverviewResponse = {
  totalDocuments: number;
  indexedDocuments: number;
  failedDocuments: number;
  deletedDocuments: number;
  totalUsers: number;
  activeUsers: number;
  totalAuditLogs: number;
  auditLogsLast24Hours: number;
  totalChatSessions: number;
  totalPromptTokens: number;
  totalCompletionTokens: number;
  totalTokens: number;
  documentStatusCounts: Array<{
    status: string;
    count: number;
  }>;
  recentAuditLogs: Array<{
    action: string;
    resourceType: string;
    createdAt: string;
  }>;
};

export type AdminUsageResponse = {
  from: string | null;
  to: string | null;
  totalRequests: number;
  totalPromptTokens: number;
  totalCompletionTokens: number;
  totalTokens: number;
  byActor: AdminUsageByActor[];
  byModel: AdminUsageByModel[];
};

export type AdminUsageByActor = {
  actorType: string;
  actorId: string | null;
  displayName: string;
  requestCount: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
};

export type AdminUsageByModel = {
  model: string;
  requestCount: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
};

export type AdminUserItem = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  departmentId: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};
