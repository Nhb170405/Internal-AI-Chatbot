import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { profileDataset } from "../../api/datasetsApi";
import {
  chunkDocument,
  getDocument,
  getDocumentMetadata,
  indexDocument,
  ingestDocument,
  listDocumentChunks
} from "../../api/documentsApi";
import { Badge } from "../../components/ui/Badge";
import type { DocumentChunk, DocumentListItem, DocumentMetadata } from "../../types/documents";

function formatBytes(value: number) {
  if (value < 1024) {
    return `${value} B`;
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`;
  }

  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}

function formatJson(value: unknown) {
  return JSON.stringify(value, null, 2);
}

function statusTone(status: string): "neutral" | "success" | "warning" | "danger" | "info" {
  if (status === "indexed" || status === "chunked" || status === "extracted") {
    return "success";
  }

  if (status === "processing") {
    return "warning";
  }

  if (status === "failed" || status === "deleted") {
    return "danger";
  }

  return "info";
}

export function DocumentDetailPage() {
  const { documentId } = useParams();
  const [document, setDocument] = useState<DocumentListItem | null>(null);
  const [metadata, setMetadata] = useState<DocumentMetadata | null>(null);
  const [chunks, setChunks] = useState<DocumentChunk[]>([]);
  const [lastActionResult, setLastActionResult] = useState<unknown>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    loadDetail();
  }, [documentId]);

  async function loadDetail() {
    if (!documentId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const [documentResult, metadataResult] = await Promise.all([
        getDocument(documentId),
        getDocumentMetadata(documentId)
      ]);

      setDocument(documentResult);
      setMetadata(metadataResult);

      try {
        const chunkResult = await listDocumentChunks(documentId);
        setChunks(chunkResult);
      } catch {
        setChunks([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được chi tiết tài liệu.");
    } finally {
      setLoading(false);
    }
  }

  async function runAction(actionName: string, action: () => Promise<unknown>) {
    setActionLoading(actionName);
    setError(null);
    setMessage(null);

    try {
      const result = await action();
      setLastActionResult(result);
      setMessage(`Đã chạy ${actionName}.`);
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : `Không chạy được ${actionName}.`);
    } finally {
      setActionLoading(null);
    }
  }

  if (!documentId) {
    return <div className="alert alert-danger">Thiếu documentId trên URL.</div>;
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Document Detail</p>
          <h1>Chi tiết tài liệu</h1>
          <p className="helper-text">Theo dõi metadata, chunks và chạy các bước xử lý tài liệu.</p>
        </div>

        <div className="row-actions">
          <Link className="button button-secondary" to="/documents">
            Quay lại
          </Link>
          <button className="button button-secondary" onClick={loadDetail} disabled={loading}>
            Làm mới
          </button>
        </div>
      </div>

      {error ? <div className="alert alert-danger">{error}</div> : null}
      {message ? <div className="alert alert-success">{message}</div> : null}

      {document ? (
        <div className="panel">
          <div className="split-header">
            <div>
              <h2>{document.originalFileName}</h2>
              <p className="helper-text">
                {document.id} · {formatBytes(document.sizeBytes)}
              </p>
            </div>
            <div className="row-actions">
              <Badge tone={statusTone(document.status)}>{document.status}</Badge>
              <Badge tone="neutral">{document.accessLevel}</Badge>
            </div>
          </div>

          <div className="action-strip">
            <button
              className="button button-secondary"
              disabled={Boolean(actionLoading)}
              onClick={() => runAction("ingest", () => ingestDocument(document.id))}
            >
              {actionLoading === "ingest" ? "Đang ingest..." : "Ingest"}
            </button>
            <button
              className="button button-secondary"
              disabled={Boolean(actionLoading)}
              onClick={() => runAction("chunk", () => chunkDocument(document.id))}
            >
              {actionLoading === "chunk" ? "Đang chunk..." : "Chunk"}
            </button>
            <button
              className="button button-secondary"
              disabled={Boolean(actionLoading)}
              onClick={() => runAction("index", () => indexDocument(document.id))}
            >
              {actionLoading === "index" ? "Đang index..." : "Index"}
            </button>
            <button
              className="button button-secondary"
              disabled={Boolean(actionLoading)}
              onClick={() => runAction("profile", () => profileDataset(document.id))}
            >
              {actionLoading === "profile" ? "Đang profile..." : "Profile dataset"}
            </button>
          </div>
        </div>
      ) : null}

      <div className="analysis-grid">
        <div className="panel">
          <h2>Metadata</h2>
          {metadata ? (
            <div className="metadata-list">
              <div>
                <span>Title</span>
                <strong>{metadata.title ?? "Chưa có"}</strong>
              </div>
              <div>
                <span>Report date</span>
                <strong>{metadata.reportDate ?? "Chưa có"}</strong>
              </div>
              <div>
                <span>Report month/year</span>
                <strong>
                  {metadata.reportMonth ?? "-"} / {metadata.reportYear ?? "-"}
                </strong>
              </div>
              <div>
                <span>Department</span>
                <strong>{metadata.department ?? "Chưa có"}</strong>
              </div>
              <div>
                <span>Language</span>
                <strong>{metadata.language}</strong>
              </div>
              <div>
                <span>Detected columns</span>
                <strong>{metadata.detectedColumns.length ? metadata.detectedColumns.join(", ") : "Chưa có"}</strong>
              </div>
              <div>
                <span>Sheet names</span>
                <strong>{metadata.sheetNames.length ? metadata.sheetNames.join(", ") : "Chưa có"}</strong>
              </div>
            </div>
          ) : (
            <p className="helper-text">Chưa tải được metadata.</p>
          )}
        </div>

        <div className="panel">
          <h2>Kết quả thao tác gần nhất</h2>
          {lastActionResult ? (
            <pre className="json-preview">{formatJson(lastActionResult)}</pre>
          ) : (
            <p className="helper-text">Sau khi bấm ingest/chunk/index/profile, response sẽ hiện ở đây.</p>
          )}
        </div>
      </div>

      <div className="panel">
        <div className="split-header">
          <h2>Chunks</h2>
          <Badge tone="info">{chunks.length}</Badge>
        </div>

        {chunks.length > 0 ? (
          <div className="chunk-list">
            {chunks.slice(0, 30).map((chunk) => (
              <article className="chunk-card" key={chunk.id}>
                <div className="message-meta">
                  <Badge tone="neutral">#{chunk.chunkIndex}</Badge>
                  <span>{chunk.characterCount} ký tự</span>
                </div>
                <p>{chunk.content}</p>
              </article>
            ))}
          </div>
        ) : (
          <p className="helper-text">Chưa có chunk hoặc bạn chưa có quyền xem chunk của tài liệu này.</p>
        )}
      </div>
    </section>
  );
}
