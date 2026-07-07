import { useEffect, useMemo, useState } from "react";
import { getAdminUsers } from "../../api/adminApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { AdminUserItem } from "../../types/admin";

export function AdminUsersPage() {
  const [users, setUsers] = useState<AdminUserItem[]>([]);
  const [keyword, setKeyword] = useState("");
  const [role, setRole] = useState("all");
  const [status, setStatus] = useState("all");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadUsers();
  }, []);

  const filteredUsers = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return users.filter((user) => {
      const matchesKeyword =
        !normalizedKeyword ||
        user.email.toLowerCase().includes(normalizedKeyword) ||
        user.displayName.toLowerCase().includes(normalizedKeyword);
      const matchesRole = role === "all" || user.role === role;
      const matchesStatus =
        status === "all" ||
        (status === "active" && user.isActive) ||
        (status === "inactive" && !user.isActive);

      return matchesKeyword && matchesRole && matchesStatus;
    });
  }, [users, keyword, role, status]);

  async function loadUsers() {
    setIsLoading(true);
    setError(null);

    try {
      setUsers(await getAdminUsers());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách user.");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Admin</p>
          <h1>Users</h1>
          <p className="helper-text">Xem user nội bộ và trạng thái tài khoản. Không trả password hash hoặc session secret ra frontend.</p>
        </div>

        <Button type="button" variant="secondary" onClick={loadUsers} disabled={isLoading}>
          Refresh
        </Button>
      </div>

      <div className="document-filter-panel users-filter-panel">
        <label className="compact-field search-field">
          <span>Search</span>
          <input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder="Email hoặc tên..." />
        </label>

        <label className="compact-field">
          <span>Role</span>
          <select value={role} onChange={(event) => setRole(event.target.value)}>
            <option value="all">all</option>
            <option value="admin">admin</option>
            <option value="employee">employee</option>
          </select>
        </label>

        <label className="compact-field">
          <span>Status</span>
          <select value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="all">all</option>
            <option value="active">active</option>
            <option value="inactive">inactive</option>
          </select>
        </label>
      </div>

      {error ? <div className="alert alert-danger">{error}</div> : null}

      <div className="metric-grid">
        <MetricCard label="Total users" value={users.length} />
        <MetricCard label="Active" value={users.filter((user) => user.isActive).length} />
        <MetricCard label="Admins" value={users.filter((user) => user.role === "admin").length} />
        <MetricCard label="Employees" value={users.filter((user) => user.role === "employee").length} />
      </div>

      <div className="table-card">
        {isLoading && users.length === 0 ? (
          <div className="panel">Đang tải users...</div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>User</th>
                <th>Role</th>
                <th>Status</th>
                <th>Department</th>
                <th>Created</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.id}>
                  <td>
                    <strong>{user.displayName}</strong>
                    <span className="table-subtext">{user.email}</span>
                    <span className="table-subtext">{user.id.slice(0, 8)}</span>
                  </td>
                  <td>
                    <Badge tone={user.role === "admin" ? "warning" : "info"}>{user.role}</Badge>
                  </td>
                  <td>
                    <Badge tone={user.isActive ? "success" : "danger"}>{user.isActive ? "active" : "inactive"}</Badge>
                  </td>
                  <td>{user.departmentId ?? "-"}</td>
                  <td>{formatDate(user.createdAt)}</td>
                  <td>{formatDate(user.updatedAt)}</td>
                </tr>
              ))}

              {filteredUsers.length === 0 && (
                <tr>
                  <td colSpan={6}>Không có user phù hợp.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      <div className="panel">
        <h2>Kế hoạch nâng cấp</h2>
        <div className="admin-checklist">
          <span>Đổi role hoặc disable user cần endpoint PATCH riêng và audit log.</span>
          <span>Không cho admin tự hạ quyền/xóa chính mình nếu chưa có xác nhận mạnh.</span>
          <span>Không bao giờ trả PasswordHash, cookie, session key hoặc token ra frontend.</span>
        </div>
      </div>
    </section>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value.toLocaleString("vi-VN")}</strong>
    </div>
  );
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("vi-VN");
}
