import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "../components/layout/AppLayout";
import { LoadingScreen } from "../components/feedback/LoadingScreen";
import { AdminAuditPage } from "../features/admin/AdminAuditPage";
import { AdminOverviewPage } from "../features/admin/AdminOverviewPage";
import { AdminUsagePage } from "../features/admin/AdminUsagePage";
import { AdminUsersPage } from "../features/admin/AdminUsersPage";
import { LoginPage } from "../features/auth/LoginPage";
import { useAuth } from "../features/auth/useAuth";
import { ChatPage } from "../features/chat/ChatPage";
import { ChartsPage } from "../features/charts/ChartsPage";
import { DocumentDetailPage } from "../features/documents/DocumentDetailPage";
import { DocumentsPage } from "../features/documents/DocumentsPage";

export function App() {
  const { currentUser, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingScreen message="Đang kiểm tra phiên đăng nhập..." />;
  }

  if (!currentUser || currentUser.role === "anonymous") {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<Navigate to="/chat" replace />} />
        <Route path="/chat" element={<ChatPage />} />
        <Route path="/documents" element={<DocumentsPage />} />
        <Route path="/documents/:documentId" element={<DocumentDetailPage />} />
        <Route path="/charts" element={<ChartsPage />} />

        {currentUser.role === "admin" && (
          <>
            <Route path="/admin" element={<AdminOverviewPage />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/usage" element={<AdminUsagePage />} />
            <Route path="/admin/audit" element={<AdminAuditPage />} />
          </>
        )}

        <Route path="*" element={<Navigate to="/chat" replace />} />
      </Routes>
    </AppLayout>
  );
}
