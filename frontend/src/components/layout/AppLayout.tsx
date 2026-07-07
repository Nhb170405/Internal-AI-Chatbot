import { Topbar } from "./Topbar";
import { Sidebar } from "./Sidebar";

type AppLayoutProps = {
  children: React.ReactNode;
};

export function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="app-shell">
      <Sidebar />
      <div className="app-main">
        <Topbar />
        <main className="page-content">{children}</main>
      </div>
    </div>
  );
}
