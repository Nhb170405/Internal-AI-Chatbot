import { useEffect, useState } from "react";
import { getAdminOverview } from "../../api/adminApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { AdminOverviewResponse } from "../../types/admin";

export function AdminOverviewPage() {
  const [overview, setOverview] = useState<AdminOverviewResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadOverview();
  }, []);

  async function loadOverview() {
    setIsLoading(true);
    setError(null);

    try {
      setOverview(await getAdminOverview());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được overview.");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Admin</p>
          <h1>Tổng quan hệ thống</h1>
          <p className="helper-text">Theo dõi nhanh tài liệu, user, token usage và hoạt động gần đây.</p>
        </div>

        <Button type="button" variant="secondary" onClick={loadOverview} disabled={isLoading}>
          Refresh
        </Button>
      </div>

      {error ? <div className="alert alert-danger">{error}</div> : null}

      <div className="metric-grid">
        <MetricCard label="Documents" value={formatNumber(overview?.totalDocuments)} subText={`${formatNumber(overview?.indexedDocuments)} indexed`} />
        <MetricCard label="Failed files" value={formatNumber(overview?.failedDocuments)} subText={`${formatNumber(overview?.deletedDocuments)} deleted`} />
        <MetricCard label="Users" value={formatNumber(overview?.totalUsers)} subText={`${formatNumber(overview?.activeUsers)} active`} />
        <MetricCard label="Tokens" value={formatNumber(overview?.totalTokens)} subText={`${formatNumber(overview?.totalChatSessions)} chat sessions`} />
      </div>

      <div className="analysis-grid">
        <div className="panel">
          <h2>Document status</h2>
          {overview?.documentStatusCounts.length ? (
            <div className="status-list">
              {overview.documentStatusCounts.map((item) => (
                <div key={item.status}>
                  <Badge tone={statusTone(item.status)}>{item.status}</Badge>
                  <strong>{item.count}</strong>
                </div>
              ))}
            </div>
          ) : (
            <p className="helper-text">Chưa có document.</p>
          )}
        </div>

        <div className="panel">
          <h2>Hoạt động gần đây</h2>
          <p className="helper-text">{formatNumber(overview?.auditLogsLast24Hours)} audit logs trong 24 giờ gần nhất.</p>

          {overview?.recentAuditLogs.length ? (
            <div className="recent-list">
              {overview.recentAuditLogs.map((log, index) => (
                <div key={`${log.action}-${log.createdAt}-${index}`}>
                  <Badge tone="info">{log.action}</Badge>
                  <span>{log.resourceType}</span>
                  <time>{formatDate(log.createdAt)}</time>
                </div>
              ))}
            </div>
          ) : (
            <p className="helper-text">Chưa có audit log.</p>
          )}
        </div>
      </div>
    </section>
  );
}

type MetricCardProps = {
  label: string;
  value: string;
  subText: string;
};

function MetricCard({ label, value, subText }: MetricCardProps) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{subText}</small>
    </div>
  );
}

function formatNumber(value?: number) {
  return typeof value === "number" ? value.toLocaleString("vi-VN") : "-";
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("vi-VN");
}

function statusTone(status: string): "neutral" | "success" | "warning" | "danger" | "info" {
  if (status === "indexed" || status === "chunked" || status === "extracted") {
    return "success";
  }

  if (status === "failed" || status === "deleted") {
    return "danger";
  }

  if (status === "processing") {
    return "warning";
  }

  return "info";
}
