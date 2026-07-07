import { useEffect, useMemo, useState } from "react";
import { analyzeDataset, getDatasetProfiles, profileDataset } from "../../api/datasetsApi";
import { listDocuments } from "../../api/documentsApi";
import { Badge } from "../../components/ui/Badge";
import type { DatasetAnalyzeResponse, DatasetProfileResponse } from "../../types/datasets";
import type { DocumentListItem } from "../../types/documents";

const tableExtensions = new Set([".csv", ".xlsx", ".xls"]);

const operations = [
  { value: "preview", label: "Preview" },
  { value: "list_columns", label: "List columns" },
  { value: "count", label: "Count rows" },
  { value: "sum", label: "Sum" },
  { value: "average", label: "Average" },
  { value: "group_by", label: "Group by" },
  { value: "top_n", label: "Top N" }
];

function isTableDocument(document: DocumentListItem) {
  return tableExtensions.has(document.extension.toLowerCase());
}

function getDocumentLabel(document: DocumentListItem) {
  return `${document.originalFileName} (${document.status}, ${document.accessLevel})`;
}

function formatJson(value: unknown) {
  return JSON.stringify(value, null, 2);
}

function ResultPreview({ result }: { result: unknown }) {
  if (Array.isArray(result) && result.length > 0 && typeof result[0] === "object") {
    const rows = result as Record<string, unknown>[];
    const headers = Object.keys(rows[0] ?? {});

    return (
      <div className="table-card compact-table">
        <table>
          <thead>
            <tr>
              {headers.map((header) => (
                <th key={header}>{header}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.slice(0, 20).map((row, index) => (
              <tr key={index}>
                {headers.map((header) => (
                  <td key={header}>{String(row[header] ?? "")}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  return <pre className="json-preview">{formatJson(result)}</pre>;
}

export function DatasetsPage() {
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [selectedDocumentId, setSelectedDocumentId] = useState("");
  const [profile, setProfile] = useState<DatasetProfileResponse | null>(null);
  const [analysis, setAnalysis] = useState<DatasetAnalyzeResponse | null>(null);
  const [operation, setOperation] = useState("preview");
  const [sheetName, setSheetName] = useState("");
  const [valueColumn, setValueColumn] = useState("");
  const [groupByColumn, setGroupByColumn] = useState("");
  const [topN, setTopN] = useState(10);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const tableDocuments = useMemo(() => documents.filter(isTableDocument), [documents]);
  const selectedDocument = tableDocuments.find((document) => document.id === selectedDocumentId) ?? null;

  const availableSheets = useMemo(() => {
    const names = profile?.profiles.map((item) => item.sheetName).filter(Boolean) ?? [];
    return Array.from(new Set(names));
  }, [profile]);

  const availableColumns = useMemo(() => {
    const matchedProfile =
      profile?.profiles.find((item) => item.sheetName === sheetName) ??
      profile?.profiles[0] ??
      null;

    return matchedProfile?.columns.map((column) => column.name) ?? [];
  }, [profile, sheetName]);

  useEffect(() => {
    loadDocuments();
  }, []);

  async function loadDocuments() {
    setLoading(true);
    setError(null);

    try {
      const result = await listDocuments();
      const tableFiles = result.filter(isTableDocument);
      setDocuments(result);

      if (!selectedDocumentId && tableFiles.length > 0) {
        setSelectedDocumentId(tableFiles[0].id);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách tài liệu.");
    } finally {
      setLoading(false);
    }
  }

  async function runProfile(mode: "read" | "refresh") {
    if (!selectedDocumentId) {
      setError("Hãy chọn một file CSV/XLSX trước.");
      return;
    }

    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const result = mode === "refresh" ? await profileDataset(selectedDocumentId) : await getDatasetProfiles(selectedDocumentId);
      setProfile(result);
      setAnalysis(null);
      setMessage(mode === "refresh" ? "Đã chạy profile và cập nhật metadata." : "Đã tải profile hiện có.");

      const firstSheet = result.profiles[0]?.sheetName ?? "";
      if (firstSheet) {
        setSheetName(firstSheet);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không lấy được profile.");
    } finally {
      setLoading(false);
    }
  }

  async function runAnalyze() {
    if (!selectedDocumentId) {
      setError("Hãy chọn một file CSV/XLSX trước.");
      return;
    }

    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const result = await analyzeDataset(selectedDocumentId, {
        operation,
        sheetName: sheetName || undefined,
        valueColumn: valueColumn || undefined,
        groupByColumn: groupByColumn || undefined,
        topN
      });

      setAnalysis(result);
      setMessage(result.success ? "Phân tích dữ liệu thành công." : result.errorMessage ?? "Phân tích dữ liệu thất bại.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không phân tích được dữ liệu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Datasets</p>
          <h1>Phân tích dữ liệu bảng</h1>
          <p className="helper-text">Chọn file CSV/XLSX, đọc cấu trúc bảng rồi chạy phép phân tích bằng pandas.</p>
        </div>

        <button className="button button-secondary" onClick={loadDocuments} disabled={loading}>
          Làm mới
        </button>
      </div>

      {error ? <div className="alert alert-danger">{error}</div> : null}
      {message ? <div className="alert alert-success">{message}</div> : null}

      <div className="toolbar-panel">
        <label className="field">
          File dữ liệu
          <select value={selectedDocumentId} onChange={(event) => setSelectedDocumentId(event.target.value)}>
            <option value="">Chọn file CSV/XLSX</option>
            {tableDocuments.map((document) => (
              <option key={document.id} value={document.id}>
                {getDocumentLabel(document)}
              </option>
            ))}
          </select>
        </label>

        <div className="row-actions">
          <button className="button button-secondary" onClick={() => runProfile("read")} disabled={loading || !selectedDocumentId}>
            Xem profile
          </button>
          <button className="button button-primary" onClick={() => runProfile("refresh")} disabled={loading || !selectedDocumentId}>
            Chạy profile
          </button>
        </div>
      </div>

      {selectedDocument ? (
        <div className="panel">
          <div className="split-header">
            <div>
              <h2>{selectedDocument.originalFileName}</h2>
              <p className="helper-text">
                {selectedDocument.extension} · {selectedDocument.status} · {selectedDocument.accessLevel}
              </p>
            </div>
            <Badge tone="info">{selectedDocument.id.slice(0, 8)}</Badge>
          </div>
        </div>
      ) : null}

      <div className="analysis-grid">
        <div className="panel">
          <h2>Cấu hình phân tích</h2>

          <div className="form-grid">
            <label className="field">
              Operation
              <select value={operation} onChange={(event) => setOperation(event.target.value)}>
                {operations.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              Sheet
              <select value={sheetName} onChange={(event) => setSheetName(event.target.value)}>
                <option value="">Sheet mặc định</option>
                {availableSheets.map((name) => (
                  <option key={name} value={name}>
                    {name}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              Value column
              <input value={valueColumn} onChange={(event) => setValueColumn(event.target.value)} placeholder="Price, Revenue..." />
            </label>

            <label className="field">
              Group by column
              <input value={groupByColumn} onChange={(event) => setGroupByColumn(event.target.value)} placeholder="Month, Category..." />
            </label>

            <label className="field">
              Top N
              <input type="number" min="1" max="100" value={topN} onChange={(event) => setTopN(Number(event.target.value))} />
            </label>
          </div>

          <div className="row-actions form-actions">
            <button className="button button-primary" onClick={runAnalyze} disabled={loading || !selectedDocumentId}>
              Phân tích
            </button>
          </div>

          {availableColumns.length > 0 ? (
            <div className="badge-list">
              {availableColumns.map((column) => (
                <button
                  key={column}
                  className="chip-button"
                  type="button"
                  onClick={() => {
                    if (!valueColumn) {
                      setValueColumn(column);
                    } else {
                      setGroupByColumn(column);
                    }
                  }}
                >
                  {column}
                </button>
              ))}
            </div>
          ) : null}
        </div>

        <div className="panel">
          <h2>Kết quả</h2>
          {analysis ? (
            <div className="stack">
              <div className="message-meta">
                <Badge tone={analysis.success ? "success" : "danger"}>{analysis.operation}</Badge>
                {analysis.rowCount !== null && analysis.rowCount !== undefined ? <span>{analysis.rowCount} rows</span> : null}
              </div>
              {analysis.errorMessage ? <div className="alert alert-danger">{analysis.errorMessage}</div> : null}
              {analysis.warnings.length > 0 ? (
                <ul className="warning-list">
                  {analysis.warnings.map((warning) => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              ) : null}
              <ResultPreview result={analysis.result} />
            </div>
          ) : (
            <p className="helper-text">Kết quả phân tích sẽ hiện ở đây.</p>
          )}
        </div>
      </div>

      <div className="panel">
        <h2>Profile bảng</h2>
        {profile?.profiles.length ? (
          <div className="profile-grid">
            {profile.profiles.map((item) => (
              <div className="table-card profile-card" key={item.id}>
                <div className="profile-card-header">
                  <div>
                    <strong>{item.sheetName || `Table ${item.tableIndex}`}</strong>
                    <p className="table-subtext">
                      {item.rowCount} rows · {item.columnCount} columns
                    </p>
                  </div>
                  <Badge tone="neutral">#{item.tableIndex}</Badge>
                </div>
                <table>
                  <thead>
                    <tr>
                      <th>Column</th>
                      <th>Type</th>
                      <th>Non-null</th>
                      <th>Null</th>
                    </tr>
                  </thead>
                  <tbody>
                    {item.columns.map((column) => (
                      <tr key={`${item.id}-${column.name}`}>
                        <td>{column.name}</td>
                        <td>{column.dataType}</td>
                        <td>{column.nonNullCount}</td>
                        <td>{column.nullCount}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ))}
          </div>
        ) : (
          <p className="helper-text">Chưa có profile. Hãy bấm “Chạy profile” cho một file CSV/XLSX.</p>
        )}
      </div>
    </section>
  );
}
