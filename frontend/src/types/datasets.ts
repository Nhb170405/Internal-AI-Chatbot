export type DatasetAnalyzeRequest = {
  operation: string;
  sheetName?: string;
  valueColumn?: string;
  groupByColumn?: string;
  topN?: number;
};

export type DatasetAnalyzeResponse = {
  documentId: string;
  success: boolean;
  operation: string;
  result: unknown;
  rowCount?: number | null;
  warnings: string[];
  errorMessage: string | null;
};

export type DatasetColumnProfile = {
  name: string;
  normalizedName: string;
  dataType: string;
  nonNullCount: number;
  nullCount: number;
};

export type DatasetTableProfile = {
  id: string;
  documentId: string;
  sheetName: string;
  tableIndex: number;
  rowCount: number;
  columnCount: number;
  columns: DatasetColumnProfile[];
  sampleRows: Record<string, unknown>[];
  warnings: string[];
  createdAt: string;
  updatedAt: string;
};

export type DatasetProfileResponse = {
  documentId: string;
  success: boolean;
  tableCount: number;
  profiles: DatasetTableProfile[];
  warnings: string[];
  errorMessage: string | null;
};
