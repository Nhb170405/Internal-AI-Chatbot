import { ChangeEvent, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import {
  deleteDocument,
  listDocuments,
  uploadDocument
} from "../../api/documentsApi";
import { getLatestJobByDocument } from "../../api/backgroundJobsApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { DocumentAccessLevel, DocumentListItem } from "../../types/documents";
import { useAuth } from "../auth/useAuth";

type DocumentFilter = {
  status: string;
  accessLevel: string;
  keyword: string;
};

type DocumentAction = "delete";

export function DocumentsPage() {
  const { currentUser } = useAuth();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [filter, setFilter] = useState<DocumentFilter>({ status: "all", accessLevel: "all", keyword: "" });
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadAccessLevel, setUploadAccessLevel] = useState<DocumentAccessLevel>("employee");
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [busyDocumentId, setBusyDocumentId] = useState<string | null>(null);
  const [trackedDocumentIds, setTrackedDocumentIds] = useState<string[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const canUpload = currentUser?.role === "employee" || currentUser?.role === "admin";
  const canManage = currentUser?.role === "employee" || currentUser?.role === "admin";

  useEffect(() => {
    void loadDocuments();
  }, []);

  useEffect(() => {
    if (trackedDocumentIds.length === 0) {
      return;
    }

    let isCancelled = false;

    async function pollProcessingJobs() {
      const remainingDocumentIds: string[] = [];
      let shouldReloadDocuments = false;
      let latestFailureMessage: string | null = null;

      for (const documentId of trackedDocumentIds) {
        try {
          const job = await getLatestJobByDocument(documentId);

          if (job.status === "queued" || job.status === "running") {
            remainingDocumentIds.push(documentId);
            continue;
          }

          shouldReloadDocuments = true;

          if (job.status === "failed") {
            latestFailureMessage = job.lastError ?? "Document processing failed.";
          }
        } catch {
          shouldReloadDocuments = true;
        }
      }

      if (isCancelled) {
        return;
      }

      setTrackedDocumentIds(remainingDocumentIds);

      if (latestFailureMessage) {
        setErrorMessage(latestFailureMessage);
        setSuccessMessage(null);
      } else if (shouldReloadDocuments) {
        setSuccessMessage("Document processing completed.");
      }

      if (shouldReloadDocuments) {
        await loadDocuments({ silent: true });
      }
    }

    const intervalId = window.setInterval(() => {
      void pollProcessingJobs();
    }, 3000);

    void pollProcessingJobs();

    return () => {
      isCancelled = true;
      window.clearInterval(intervalId);
    };
  }, [trackedDocumentIds]);

  const filteredDocuments = useMemo(() => {
    return documents.filter((document) => {
      const matchesStatus = filter.status === "all" || document.status === filter.status;
      const matchesAccess = filter.accessLevel === "all" || document.accessLevel === filter.accessLevel;
      const matchesKeyword =
        !filter.keyword.trim() ||
        document.originalFileName.toLowerCase().includes(filter.keyword.trim().toLowerCase());

      return matchesStatus && matchesAccess && matchesKeyword;
    });
  }, [documents, filter]);

  async function loadDocuments(options: { silent?: boolean } = {}) {
    if (!options.silent) {
      setIsLoading(true);
      setErrorMessage(null);
    }

    try {
      const result = await listDocuments();
      setDocuments(result);
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : "Không tải được danh sách tài liệu.");
    } finally {
      if (!options.silent) {
        setIsLoading(false);
      }
    }
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    setSelectedFile(event.target.files?.[0] ?? null);
  }

  async function handleUpload() {
    if (!selectedFile) {
      setErrorMessage("Hãy chọn file trước khi upload.");
      return;
    }

    setIsUploading(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const uploadedDocument = await uploadDocument(selectedFile, uploadAccessLevel);
      setSelectedFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      setTrackedDocumentIds((current) => Array.from(new Set([...current, uploadedDocument.id])));
      setSuccessMessage("Upload thành công. Background job đang tự xử lý ingest, chunk, index và profile nếu là file bảng.");
      await loadDocuments();
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : "Upload hoặc xử lý tài liệu thất bại. Kiểm tra file, Python service, Qdrant hoặc OpenAI key.");
      await loadDocuments();
    } finally {
      setIsUploading(false);
    }
  }

  async function runDocumentAction(document: DocumentListItem, action: DocumentAction) {
    setBusyDocumentId(document.id);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await deleteDocument(document.id);
      setDocuments((current) => current.filter((item) => item.id !== document.id));
      setSuccessMessage("Đã xóa tài liệu.");
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : `Action ${action} thất bại.`);
    } finally {
      setBusyDocumentId(null);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Documents</p>
          <h1>Kho tài liệu</h1>
          <p className="helper-text">Upload, lọc và chạy pipeline xử lý tài liệu nội bộ.</p>
        </div>
        <Button type="button" variant="secondary" onClick={() => void loadDocuments()}>
          Refresh
        </Button>
      </div>

      {canUpload && (
        <div className="document-upload-panel">
          <div className="upload-card">
            <input ref={fileInputRef} className="visually-hidden" type="file" onChange={handleFileChange} />
            <button className="file-select-button" type="button" onClick={() => fileInputRef.current?.click()}>
              Chọn file
            </button>
            <div>
              <strong>{selectedFile?.name ?? "Chưa chọn file"}</strong>
              <span>{selectedFile ? formatBytes(selectedFile.size) : "PDF, DOCX, CSV, XLSX..."}</span>
            </div>
          </div>

          <label className="compact-field">
            <span>Access level</span>
            <select
              value={uploadAccessLevel}
              onChange={(event) => setUploadAccessLevel(event.target.value as DocumentAccessLevel)}
            >
              {currentUser?.role === "admin" && <option value="admin">admin</option>}
              <option value="employee">employee</option>
              <option value="guest">guest</option>
            </select>
          </label>

          <Button type="button" onClick={handleUpload} disabled={isUploading}>
            {isUploading ? "Uploading..." : "Upload"}
          </Button>
        </div>
      )}

      <div className="document-filter-panel">
        <label className="compact-field search-field">
          <span>Search</span>
          <input
            value={filter.keyword}
            onChange={(event) => setFilter((current) => ({ ...current, keyword: event.target.value }))}
            placeholder="Tên file..."
          />
        </label>

        <label className="compact-field">
          <span>Status</span>
          <select
            value={filter.status}
            onChange={(event) => setFilter((current) => ({ ...current, status: event.target.value }))}
          >
            <option value="all">all</option>
            <option value="uploaded">uploaded</option>
            <option value="extracted">extracted</option>
            <option value="chunked">chunked</option>
            <option value="indexed">indexed</option>
            <option value="failed">failed</option>
          </select>
        </label>

        <label className="compact-field">
          <span>Access</span>
          <select
            value={filter.accessLevel}
            onChange={(event) => setFilter((current) => ({ ...current, accessLevel: event.target.value }))}
          >
            <option value="all">all</option>
            <option value="admin">admin</option>
            <option value="employee">employee</option>
            <option value="guest">guest</option>
          </select>
        </label>
      </div>

      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {successMessage && <div className="alert alert-success">{successMessage}</div>}

      <div className="table-card document-table-card">
        {isLoading ? (
          <div className="panel">Đang tải tài liệu...</div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Tên file</th>
                <th>Kích thước</th>
                <th>Loại</th>
                <th>Quyền</th>
                <th>Trạng thái</th>
                <th>Cập nhật</th>
                {canManage && <th>Actions</th>}
              </tr>
            </thead>
            <tbody>
              {filteredDocuments.map((document) => (
                <tr key={document.id}>
                  <td>
                    <Link className="document-name-link" to={`/documents/${document.id}`}>
                      {document.originalFileName}
                    </Link>
                    <span className="table-subtext">{document.id.slice(0, 8)}</span>
                  </td>
                  <td>{formatBytes(document.sizeBytes)}</td>
                  <td>{document.extension}</td>
                  <td>{document.accessLevel}</td>
                  <td>
                    <div className="status-badge-stack">
                      <Badge tone={statusTone(document.status)}>{document.status}</Badge>
                      {document.hasTableProfile ? <Badge tone="success">profiled</Badge> : null}
                    </div>
                  </td>
                  <td>{formatDate(document.updatedAt ?? document.createdAt ?? document.uploadedAt)}</td>
                  {canManage && (
                    <td>
                      <div className="document-actions">
                        {(["delete"] as DocumentAction[]).map((action) => {
                          const state = getActionState(document, action, currentUser?.role);
                          return (
                            <button
                              key={action}
                              className={state.className}
                              title={state.title}
                              onClick={() => runDocumentAction(document, action)}
                              disabled={busyDocumentId === document.id || !state.enabled}
                            >
                              {action}
                            </button>
                          );
                        })}
                      </div>
                    </td>
                  )}
                </tr>
              ))}

              {filteredDocuments.length === 0 && (
                <tr>
                  <td colSpan={canManage ? 7 : 6}>Không có tài liệu phù hợp.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </section>
  );
}

function getActionState(document: DocumentListItem, action: DocumentAction, role?: string) {
  const isDeleted = document.status === "deleted";

  return {
    enabled: !isDeleted && role === "admin",
    className: role === "admin" ? "action-danger" : "action-muted",
    title: role === "admin" ? "Soft delete tài liệu." : "Employee chỉ xóa được file của mình, frontend hiện chưa có owner để xác định chắc."
  };
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

function formatDate(value?: string) {
  if (!value) {
    return "-";
  }

  return new Date(value).toLocaleString("vi-VN");
}

function formatBytes(value: number) {
  if (value < 1024) {
    return `${value} B`;
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`;
  }

  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}
