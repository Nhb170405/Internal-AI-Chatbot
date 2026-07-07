import { FormEvent, useEffect, useMemo, useState } from "react";
import { getAdminAuditLogs } from "../../api/adminApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { AdminAuditLogItem, AdminAuditLogsResponse } from "../../types/admin";

const pageSize = 30;

export function AdminAuditPage() {
  const [logs, setLogs] = useState<AdminAuditLogsResponse | null>(null);
  const [action, setAction] = useState("");
  const [resourceType, setResourceType] = useState("");
  const [actorId, setActorId] = useState("");
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = useMemo(() => {
    if (!logs) {
      return 1;
    }

    return Math.max(1, Math.ceil(logs.totalCount / logs.pageSize));
  }, [logs]);

  useEffect(() => {
    void loadLogs(page);
  }, [page]);

  async function loadLogs(targetPage = 1) {
    setIsLoading(true);
    setError(null);

    try {
      const result = await getAdminAuditLogs({
        action: action.trim() || undefined,
        resourceType: resourceType.trim() || undefined,
        actorId: actorId.trim() || undefined,
        page: targetPage,
        pageSize
      });
      setLogs(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được audit logs.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleFilter(event: FormEvent) {
    event.preventDefault();
    if (page === 1) {
      void loadLogs(1);
      return;
    }

    setPage(1);
  }

  function resetFilters() {
    setAction("");
    setResourceType("");
    setActorId("");
    setPage(1);
    setTimeout(() => void loadLogs(1), 0);
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Admin</p>
          <h1>Audit Logs</h1>
          <p className="helper-text">Theo dõi hành động quan trọng trong hệ thống mà không mở trực tiếp database.</p>
        </div>

        <Button type="button" variant="secondary" onClick={() => loadLogs(page)} disabled={isLoading}>
          Refresh
        </Button>
      </div>

      <form className="document-filter-panel audit-filter-panel" onSubmit={handleFilter}>
        <label className="compact-field">
          <span>Action</span>
          <input value={action} onChange={(event) => setAction(event.target.value)} placeholder="document_upload, chat..." />
        </label>

        <label className="compact-field">
          <span>Resource type</span>
          <input value={resourceType} onChange={(event) => setResourceType(event.target.value)} placeholder="document, chat..." />
        </label>

        <label className="compact-field">
          <span>Actor id</span>
          <input value={actorId} onChange={(event) => setActorId(event.target.value)} placeholder="UserId hoặc GuestSessionId" />
        </label>

        <div className="row-actions audit-filter-actions">
          <Button type="submit" disabled={isLoading}>
            Lọc
          </Button>
          <Button type="button" variant="secondary" onClick={resetFilters} disabled={isLoading}>
            Xóa lọc
          </Button>
        </div>
      </form>

      {error ? <div className="alert alert-danger">{error}</div> : null}

      <div className="table-card audit-table-card">
        {isLoading && !logs ? (
          <div className="panel">Đang tải audit logs...</div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Action</th>
                <th>Resource</th>
                <th>Actor</th>
                <th>IP</th>
                <th>Metadata</th>
              </tr>
            </thead>
            <tbody>
              {(logs?.items ?? []).map((log) => (
                <tr key={log.id}>
                  <td>{formatDate(log.createdAt)}</td>
                  <td>
                    <Badge tone={actionTone(log.action)}>{log.action}</Badge>
                  </td>
                  <td>
                    <strong>{log.resourceType}</strong>
                    <span className="table-subtext">{log.resourceId ?? "-"}</span>
                  </td>
                  <td>
                    <ActorCell log={log} />
                  </td>
                  <td>{log.ipAddress ?? "-"}</td>
                  <td>
                    <MetadataPreview value={log.metadataJson} />
                  </td>
                </tr>
              ))}

              {logs?.items.length === 0 && (
                <tr>
                  <td colSpan={6}>Không có audit log phù hợp.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      <div className="pagination-bar">
        <span>
          Trang {logs?.page ?? page}/{totalPages} - {logs?.totalCount ?? 0} logs
        </span>
        <div className="row-actions">
          <Button type="button" variant="secondary" disabled={isLoading || page <= 1} onClick={() => setPage((current) => current - 1)}>
            Trước
          </Button>
          <Button type="button" variant="secondary" disabled={isLoading || page >= totalPages} onClick={() => setPage((current) => current + 1)}>
            Sau
          </Button>
        </div>
      </div>
    </section>
  );
}

function ActorCell({ log }: { log: AdminAuditLogItem }) {
  if (log.actorUserId) {
    return (
      <>
        <strong>User</strong>
        <span className="table-subtext">{log.actorUserId}</span>
      </>
    );
  }

  if (log.actorGuestSessionId) {
    return (
      <>
        <strong>Guest</strong>
        <span className="table-subtext">{log.actorGuestSessionId}</span>
      </>
    );
  }

  return <span>-</span>;
}

function MetadataPreview({ value }: { value: string | null }) {
  if (!value) {
    return <span>-</span>;
  }

  return (
    <details className="metadata-preview">
      <summary>Xem</summary>
      <pre>{formatMetadata(value)}</pre>
    </details>
  );
}

function formatMetadata(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function actionTone(action: string): "neutral" | "success" | "warning" | "danger" | "info" {
  if (action.includes("failed") || action.includes("delete")) {
    return "danger";
  }

  if (action.includes("completed") || action.includes("success") || action.includes("login")) {
    return "success";
  }

  if (action.includes("route") || action.includes("chat")) {
    return "info";
  }

  return "neutral";
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("vi-VN");
}
