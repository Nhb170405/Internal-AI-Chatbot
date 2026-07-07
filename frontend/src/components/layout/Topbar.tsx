import { LogOut } from "lucide-react";
import { useLocation } from "react-router-dom";
import { Button } from "../ui/Button";
import { useAuth } from "../../features/auth/useAuth";

const pageTitles: Record<string, { title: string; subtitle: string }> = {
  "/chat": {
    title: "Assistant Chat",
    subtitle: "Tự định tuyến giữa chat thường, RAG, dataset và chart."
  },
  "/documents": {
    title: "Documents",
    subtitle: "Upload, lọc và xử lý tài liệu nội bộ."
  },
  "/datasets": {
    title: "Datasets",
    subtitle: "Phân tích dữ liệu bảng bằng pandas."
  },
  "/charts": {
    title: "Charts",
    subtitle: "Tạo biểu đồ từ dữ liệu bảng."
  },
  "/admin": {
    title: "Admin",
    subtitle: "Khu vực vận hành hệ thống."
  }
};

export function Topbar() {
  const { currentUser, logoutCurrentUser } = useAuth();
  const location = useLocation();
  const page = getPageContext(location.pathname);

  return (
    <header className="topbar">
      <div className="topbar-context">
        <strong>{page.title}</strong>
        <span>{page.subtitle}</span>
      </div>

      <div className="topbar-user">
        <div>
          <strong>{currentUser?.displayName}</strong>
          <span>{currentUser?.email ?? currentUser?.role}</span>
        </div>
        <Button type="button" variant="ghost" onClick={logoutCurrentUser} title="Đăng xuất">
          <LogOut size={18} />
        </Button>
      </div>
    </header>
  );
}

function getPageContext(pathname: string) {
  if (pathname.startsWith("/documents/")) {
    return {
      title: "Document Detail",
      subtitle: "Theo dõi metadata, chunks và pipeline của một tài liệu."
    };
  }

  if (pathname.startsWith("/admin/")) {
    return pageTitles["/admin"];
  }

  return pageTitles[pathname] ?? pageTitles["/chat"];
}
