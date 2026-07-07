import { useEffect, useMemo, useState } from "react";
import { buildApiUrl } from "../../api/httpClient";
import { createChart } from "../../api/chartsApi";
import { getDatasetProfiles, profileDataset } from "../../api/datasetsApi";
import { listDocuments } from "../../api/documentsApi";
import { Badge } from "../../components/ui/Badge";
import type { ChartResponse, ChartType } from "../../types/charts";
import type { DatasetProfileResponse } from "../../types/datasets";
import type { DocumentListItem } from "../../types/documents";

const tableExtensions = new Set([".csv", ".xlsx", ".xls"]);

function isTableDocument(document: DocumentListItem) {
  return tableExtensions.has(document.extension.toLowerCase());
}

function formatJson(value: unknown) {
  return JSON.stringify(value, null, 2);
}

export function ChartsPage() {
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [selectedDocumentId, setSelectedDocumentId] = useState("");
  const [profile, setProfile] = useState<DatasetProfileResponse | null>(null);
  const [chart, setChart] = useState<ChartResponse | null>(null);
  const [chartType, setChartType] = useState<ChartType>("bar");
  const [operation, setOperation] = useState("group_by");
  const [sheetName, setSheetName] = useState("");
  const [valueColumn, setValueColumn] = useState("");
  const [groupByColumn, setGroupByColumn] = useState("");
  const [topN, setTopN] = useState(10);
  const [title, setTitle] = useState("");
  const [xField, setXField] = useState("");
  const [yField, setYField] = useState("");
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

  async function loadProfile(refresh: boolean) {
    if (!selectedDocumentId) {
      setError("Hãy chọn một file CSV/XLSX trước.");
      return;
    }

    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const result = refresh ? await profileDataset(selectedDocumentId) : await getDatasetProfiles(selectedDocumentId);
      setProfile(result);
      setMessage(refresh ? "Đã chạy profile cho file." : "Đã tải profile hiện có.");

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

  async function submitChart() {
    if (!selectedDocumentId) {
      setError("Hãy chọn một file CSV/XLSX trước.");
      return;
    }

    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const result = await createChart(selectedDocumentId, {
        chartType,
        operation,
        sheetName: sheetName || undefined,
        valueColumn: valueColumn || undefined,
        groupByColumn: groupByColumn || undefined,
        topN,
        title: title || undefined,
        xField: xField || undefined,
        yField: yField || undefined
      });

      setChart(result);
      setMessage(result.success ? "Đã tạo chart thành công." : result.errorMessage ?? "Tạo chart thất bại.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tạo được chart.");
    } finally {
      setLoading(false);
    }
  }

  function pickColumn(column: string) {
    if (!groupByColumn) {
      setGroupByColumn(column);
      setXField(column);
      return;
    }

    setValueColumn(column);
    setYField(column);
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Charts</p>
          <h1>Tạo biểu đồ từ dữ liệu</h1>
          <p className="helper-text">Backend phân tích bảng bằng pandas, Python render chart và trả về đường dẫn ảnh đã tạo.</p>
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
                {document.originalFileName} ({document.status}, {document.accessLevel})
              </option>
            ))}
          </select>
        </label>

        <div className="row-actions">
          <button className="button button-secondary" onClick={() => loadProfile(false)} disabled={loading || !selectedDocumentId}>
            Xem profile
          </button>
          <button className="button button-secondary" onClick={() => loadProfile(true)} disabled={loading || !selectedDocumentId}>
            Chạy profile
          </button>
        </div>
      </div>

      <div className="analysis-grid">
        <div className="panel">
          <h2>Cấu hình chart</h2>

          {selectedDocument ? (
            <p className="helper-text">
              Đang chọn: <strong>{selectedDocument.originalFileName}</strong>
            </p>
          ) : null}

          <div className="form-grid">
            <label className="field">
              Chart type
              <select value={chartType} onChange={(event) => setChartType(event.target.value as ChartType)}>
                <option value="bar">Bar</option>
                <option value="line">Line</option>
                <option value="pie">Pie</option>
              </select>
            </label>

            <label className="field">
              Operation
              <select value={operation} onChange={(event) => setOperation(event.target.value)}>
                <option value="group_by">Group by</option>
                <option value="top_n">Top N</option>
                <option value="sum">Sum</option>
                <option value="average">Average</option>
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
              Group by column
              <input value={groupByColumn} onChange={(event) => setGroupByColumn(event.target.value)} placeholder="Month, Category..." />
            </label>

            <label className="field">
              Value column
              <input value={valueColumn} onChange={(event) => setValueColumn(event.target.value)} placeholder="Revenue, Price..." />
            </label>

            <label className="field">
              Top N
              <input type="number" min="1" max="100" value={topN} onChange={(event) => setTopN(Number(event.target.value))} />
            </label>

            <label className="field">
              Title
              <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Doanh thu theo tháng" />
            </label>

            <label className="field">
              X field
              <input value={xField} onChange={(event) => setXField(event.target.value)} placeholder="Tự điền nếu để trống" />
            </label>

            <label className="field">
              Y field
              <input value={yField} onChange={(event) => setYField(event.target.value)} placeholder="Tự điền nếu để trống" />
            </label>
          </div>

          <div className="row-actions form-actions">
            <button className="button button-primary" onClick={submitChart} disabled={loading || !selectedDocumentId}>
              Tạo chart
            </button>
          </div>

          {availableColumns.length > 0 ? (
            <div className="badge-list">
              {availableColumns.map((column) => (
                <button className="chip-button" type="button" key={column} onClick={() => pickColumn(column)}>
                  {column}
                </button>
              ))}
            </div>
          ) : (
            <p className="helper-text">Chạy profile để chọn nhanh tên cột.</p>
          )}
        </div>

        <div className="panel">
          <h2>Kết quả chart</h2>
          {chart ? (
            <div className="stack">
              <div className="message-meta">
                <Badge tone={chart.success ? "success" : "danger"}>{chart.chartType}</Badge>
                <span>{chart.success ? "success" : "failed"}</span>
              </div>

              {chart.chartUrl ? (
                <figure className="chart-preview">
                  <img src={buildApiUrl(chart.chartUrl)} alt="Generated chart" />
                </figure>
              ) : null}

              {chart.chartPath ? (
                <div className="notice-box">
                  <strong>Chart path</strong>
                  <code>{chart.chartPath}</code>
                  <p>Frontend xem ảnh qua chartUrl, không đọc trực tiếp đường dẫn local.</p>
                </div>
              ) : null}

              {chart.errorMessage ? <div className="alert alert-danger">{chart.errorMessage}</div> : null}

              {chart.warnings.length > 0 ? (
                <ul className="warning-list">
                  {chart.warnings.map((warning) => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              ) : null}

              <pre className="json-preview">{formatJson(chart.data)}</pre>
            </div>
          ) : (
            <p className="helper-text">Chart path và dữ liệu đầu vào chart sẽ hiện ở đây.</p>
          )}
        </div>
      </div>
    </section>
  );
}
