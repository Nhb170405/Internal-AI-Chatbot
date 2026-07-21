import { BarChart3, Bot, FileText, LayoutDashboard, ScrollText, Users } from "lucide-react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../../features/auth/useAuth";

export function Sidebar() {
  const { currentUser } = useAuth();
  const role = currentUser?.role;

  return (
    <aside className="sidebar">
      <div className="brand">
        <span className="brand-mark">FC</span>
        <div>
          <strong>Factory Chatbot</strong>
          <span>{role}</span>
        </div>
      </div>

      <nav className="nav-list">
        <NavItem to="/chat" icon={<Bot size={18} />} label="Chat" />
        <NavItem to="/documents" icon={<FileText size={18} />} label="Documents" />
        <NavItem to="/charts" icon={<BarChart3 size={18} />} label="Charts" />

        {role === "admin" && (
          <>
            <div className="nav-section">Admin</div>
            <NavItem to="/admin" icon={<LayoutDashboard size={18} />} label="Overview" end />
            <NavItem to="/admin/users" icon={<Users size={18} />} label="Users" />
            <NavItem to="/admin/usage" icon={<BarChart3 size={18} />} label="Usage" />
            <NavItem to="/admin/audit" icon={<ScrollText size={18} />} label="Audit Logs" />
          </>
        )}
      </nav>
    </aside>
  );
}

type NavItemProps = {
  to: string;
  icon: React.ReactNode;
  label: string;
  end?: boolean;
};

function NavItem({ to, icon, label, end = false }: NavItemProps) {
  return (
    <NavLink className={({ isActive }) => `nav-item ${isActive ? "active" : ""}`} to={to} end={end}>
      {icon}
      <span>{label}</span>
    </NavLink>
  );
}
