import { FormEvent, useEffect, useState } from "react";
import { getAdminUsage } from "../../api/adminApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { AdminUsageResponse } from "../../types/admin";

export function AdminUsagePage() {
  const [usage, setUsage] = useState<AdminUsageResponse | null>(null);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadUsage();
  }, []);

  async function loadUsage() {
    setIsLoading(true);
    setError(null);

    try {
      setUsage(await getAdminUsage({
        from: toIsoStart(from),
        to: toIsoEnd(to)
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được usage.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleFilter(event: FormEvent) {
    event.preventDefault();
    void loadUsage();
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Admin</p>
          <h1>Usage</h1>
          <p className="helper-text">Theo dõi lượng token đã dùng theo user/guest và model.</p>
        </div>

        <Button type="button" variant="secondary" onClick={loadUsage} disabled={isLoading}>
          Refresh
        </Button>
      </div>

      <form className="document-filter-panel usage-filter-panel" onSubmit={handleFilter}>
        <label className="compact-field">
          <span>Từ ngày</span>
          <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
        </label>

        <label className="compact-field">
          <span>Đến ngày</span>
          <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        </label>

        <div className="row-actions audit-filter-actions">
          <Button type="submit" disabled={isLoading}>
            Lọc
          </Button>
          <Button
            type="button"
            variant="secondary"
            disabled={isLoading}
            onClick={() => {
              setFrom("");
              setTo("");
              setTimeout(() => void loadUsage(), 0);
            }}
          >
            Xóa lọc
          </Button>
        </div>
      </form>

      {error ? <div className="alert alert-danger">{error}</div> : null}

      <div className="metric-grid">
        <MetricCard label="Requests" value={formatNumber(usage?.totalRequests)} />
        <MetricCard label="Prompt tokens" value={formatNumber(usage?.totalPromptTokens)} />
        <MetricCard label="Completion tokens" value={formatNumber(usage?.totalCompletionTokens)} />
        <MetricCard label="Total tokens" value={formatNumber(usage?.totalTokens)} />
      </div>

      <div className="analysis-grid">
        <div className="panel">
          <h2>Usage theo actor</h2>
          <div className="table-card compact-table">
            <table>
              <thead>
                <tr>
                  <th>Actor</th>
                  <th>Requests</th>
                  <th>Prompt</th>
                  <th>Completion</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {(usage?.byActor ?? []).map((item) => (
                  <tr key={`${item.actorType}-${item.actorId ?? "unknown"}`}>
                    <td>
                      <Badge tone={item.actorType === "user" ? "info" : "neutral"}>{item.actorType}</Badge>
                      <span className="table-subtext">{item.displayName}</span>
                    </td>
                    <td>{formatNumber(item.requestCount)}</td>
                    <td>{formatNumber(item.promptTokens)}</td>
                    <td>{formatNumber(item.completionTokens)}</td>
                    <td>{formatNumber(item.totalTokens)}</td>
                  </tr>
                ))}

                {usage?.byActor.length === 0 && (
                  <tr>
                    <td colSpan={5}>Chưa có usage.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="panel">
          <h2>Usage theo model</h2>
          <div className="table-card compact-table">
            <table>
              <thead>
                <tr>
                  <th>Model</th>
                  <th>Requests</th>
                  <th>Total tokens</th>
                </tr>
              </thead>
              <tbody>
                {(usage?.byModel ?? []).map((item) => (
                  <tr key={item.model}>
                    <td>{item.model}</td>
                    <td>{formatNumber(item.requestCount)}</td>
                    <td>{formatNumber(item.totalTokens)}</td>
                  </tr>
                ))}

                {usage?.byModel.length === 0 && (
                  <tr>
                    <td colSpan={3}>Chưa có usage theo model.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </section>
  );
}

type MetricCardProps = {
  label: string;
  value: string;
};

function MetricCard({ label, value }: MetricCardProps) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function formatNumber(value?: number) {
  return typeof value === "number" ? value.toLocaleString("vi-VN") : "-";
}

function toIsoStart(value: string) {
  return value ? new Date(`${value}T00:00:00`).toISOString() : undefined;
}

function toIsoEnd(value: string) {
  return value ? new Date(`${value}T23:59:59`).toISOString() : undefined;
}
